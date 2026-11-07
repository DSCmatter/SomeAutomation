using System.Text.Json;
using OpenQA.Selenium;

namespace ResilientForm;

public sealed record LocatedField(IWebElement Element, string Caption, int Score);

public sealed class FieldFinder
{
    public const int AcceptScore = 80;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IWebDriver _driver;

    public FieldFinder(IWebDriver driver)
    {
        _driver = driver;
    }

    public LocatedField Require(FieldIntent intent)
    {
        _driver.SwitchTo().DefaultContent();
        var located = FindInCurrentDocument(intent);
        if (located != null)
        {
            return located;
        }

        var frames = _driver.FindElements(By.CssSelector("iframe, frame"));
        for (var i = 0; i < frames.Count; i++)
        {
            _driver.SwitchTo().DefaultContent();
            var frame = _driver.FindElements(By.CssSelector("iframe, frame"))[i];
            try
            {
                _driver.SwitchTo().Frame(frame);
            }
            catch (WebDriverException)
            {
                continue;
            }

            located = FindInCurrentDocument(intent);
            if (located != null)
            {
                return located;
            }
        }

        _driver.SwitchTo().DefaultContent();
        throw new FieldNotFoundException(
            $"No visible {KindName(intent.Kind)} matched \"{intent.Label}\" with score {AcceptScore} or higher.");
    }

    private LocatedField? FindInCurrentDocument(FieldIntent intent)
    {
        var js = (IJavaScriptExecutor)_driver;
        var json = js.ExecuteScript(ScanScript) as string;
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var controls = JsonSerializer.Deserialize<List<ControlRecord>>(json, JsonOptions) ?? [];
        LocatedField? best = null;
        foreach (var control in controls)
        {
            if (!KindMatches(intent.Kind, control))
            {
                continue;
            }

            var (score, caption) = ScoreControl(intent, control);
            if (score < AcceptScore)
            {
                continue;
            }

            if (best != null && score <= best.Score)
            {
                continue;
            }

            var element = js.ExecuteScript(
                "return window.__resilientControls ? window.__resilientControls[arguments[0]] : null;",
                control.Index) as IWebElement;
            if (element == null)
            {
                continue;
            }

            best = new LocatedField(element, caption, score);
        }

        return best;
    }

    private static (int Score, string Caption) ScoreControl(FieldIntent intent, ControlRecord control)
    {
        switch (intent.Kind)
        {
            case ControlKind.Radio:
            {
                var option = TextMatch.Best(control.Option, intent.Value, 100);
                var group = TextMatch.Best(control.Group, intent.Label, 100);
                var caption = $"{intent.Label} / {intent.Value}";
                if (option < AcceptScore || group < AcceptScore)
                {
                    return (0, caption);
                }

                return (Math.Min(option, group), caption);
            }
            case ControlKind.Text:
            case ControlKind.Select:
            {
                var strong = TextMatch.Best(control.Strong, intent.Label, 100);
                var weak = TextMatch.Best(control.Weak, intent.Label, 40);
                var score = Math.Max(strong, weak);
                var matched = strong >= weak
                    ? FirstMatching(control.Strong, intent.Label)
                    : FirstMatching(control.Weak, intent.Label);
                return (score, matched);
            }
            default:
                throw Unhandled(intent.Kind);
        }
    }

    private static string FirstMatching(IEnumerable<string>? texts, string wanted)
    {
        var caption = "";
        var best = 0;
        if (texts == null)
        {
            return caption;
        }

        foreach (var text in texts)
        {
            var score = TextMatch.Score(wanted, text);
            if (score > best)
            {
                best = score;
                caption = text.Trim();
            }
        }

        return caption;
    }

    private static bool KindMatches(ControlKind kind, ControlRecord control)
    {
        var tag = control.Tag ?? "";
        var type = control.Type ?? "";
        return kind switch
        {
            ControlKind.Select => tag == "select",
            ControlKind.Radio => tag == "input" && type == "radio",
            ControlKind.Text => tag == "textarea"
                || (tag == "input" && type is "" or "text" or "email" or "search" or "tel" or "number" or "password"),
            _ => throw Unhandled(kind),
        };
    }

    private static string KindName(ControlKind kind)
    {
        return kind switch
        {
            ControlKind.Text => "text field",
            ControlKind.Radio => "radio option",
            ControlKind.Select => "dropdown",
            _ => throw Unhandled(kind),
        };
    }

    private static Exception Unhandled(ControlKind kind)
    {
        return new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled control kind.");
    }

    private sealed class ControlRecord
    {
        public int Index { get; set; }

        public string? Tag { get; set; }

        public string? Type { get; set; }

        public List<string>? Strong { get; set; }

        public List<string>? Weak { get; set; }

        public List<string>? Option { get; set; }

        public List<string>? Group { get; set; }
    }

    private const string ScanScript = """
        const elements = [];
        const meta = [];

        function clearMarks(root) {
          if (!root || !root.querySelectorAll) return;
          for (const el of root.querySelectorAll('[data-resilient-id]')) {
            el.removeAttribute('data-resilient-id');
          }
          for (const el of root.querySelectorAll('*')) {
            if (el.shadowRoot) clearMarks(el.shadowRoot);
          }
        }

        function visible(el) {
          const style = window.getComputedStyle(el);
          if (style.display === 'none' || style.visibility === 'hidden' || Number(style.opacity) === 0) return false;
          const rect = el.getBoundingClientRect();
          return rect.width >= 1 && rect.height >= 1;
        }

        function clean(value) {
          return (value || '').replace(/\s+/g, ' ').trim();
        }

        function shortText(el) {
          if (!el || el.nodeType !== 1) return '';
          if (el.matches('input, textarea, select, button, script, style')) return '';
          if (el.querySelector('input, textarea, select')) return '';
          const text = clean(el.innerText || el.textContent || '');
          if (!text || text.length > 60) return '';
          return text;
        }

        function add(node, sink) {
          const text = shortText(node);
          if (text) sink.push(text);
        }

        function nearby(el) {
          const found = [];
          const aria = clean(el.getAttribute('aria-label'));
          if (aria) found.push(aria);
          const labelledby = el.getAttribute('aria-labelledby');
          if (labelledby) {
            for (const id of labelledby.split(/\s+/)) {
              const node = el.getRootNode().getElementById ? el.getRootNode().getElementById(id) : document.getElementById(id);
              add(node, found);
            }
          }
          if (el.labels) {
            for (const label of el.labels) add(label, found);
          }
          let sib = el.previousElementSibling;
          for (let hop = 0; sib && hop < 3; hop += 1, sib = sib.previousElementSibling) {
            if (sib.matches('input, textarea, select, button')) break;
            add(sib, found);
          }
          sib = el.nextElementSibling;
          for (let hop = 0; sib && hop < 2; hop += 1, sib = sib.nextElementSibling) {
            if (sib.matches('input, textarea, select, button')) break;
            add(sib, found);
          }
          let parent = el.parentElement;
          for (let depth = 0; parent && depth < 5; depth += 1, parent = parent.parentElement) {
            if (parent.matches('form, body, html')) break;
            let previous = parent.previousElementSibling;
            for (let hop = 0; previous && hop < 2; hop += 1, previous = previous.previousElementSibling) {
              add(previous, found);
            }
            if (parent.matches('fieldset')) {
              add(parent.querySelector(':scope > legend'), found);
            }
          }
          return found;
        }

        function optionTexts(el) {
          const found = [];
          const next = el.nextElementSibling;
          if (next && !next.matches('input, textarea, select, button')) {
            const text = clean(next.innerText || next.textContent || '');
            if (text && text.length <= 40) found.push(text);
          }
          const value = clean(el.getAttribute('value'));
          if (value) found.push(value);
          const aria = clean(el.getAttribute('aria-label'));
          if (aria) found.push(aria);
          if (el.labels) {
            for (const label of el.labels) {
              const text = clean(label.innerText || label.textContent || '');
              if (text && text.length <= 40) found.push(text);
            }
          }
          return found;
        }

        function weak(el) {
          const found = [];
          for (const name of ['placeholder', 'name', 'id']) {
            const text = clean(el.getAttribute(name));
            if (text) found.push(text);
          }
          return found;
        }

        function describe(el) {
          if (!visible(el) || el.disabled) return;
          const tag = el.tagName.toLowerCase();
          const type = (el.getAttribute('type') || '').toLowerCase();
          if (tag === 'input' && ['hidden', 'submit', 'button', 'image', 'reset', 'file', 'checkbox'].includes(type)) return;
          const index = elements.length;
          el.setAttribute('data-resilient-id', String(index));
          elements.push(el);
          meta.push({
            index,
            tag,
            type,
            strong: nearby(el),
            weak: weak(el),
            option: tag === 'input' && type === 'radio' ? optionTexts(el) : [],
            group: tag === 'input' && type === 'radio' ? nearby(el) : [],
          });
        }

        function collect(root) {
          if (!root || !root.querySelectorAll) return;
          for (const el of root.querySelectorAll('input, textarea, select')) describe(el);
          for (const el of root.querySelectorAll('*')) {
            if (el.shadowRoot) collect(el.shadowRoot);
          }
        }

        clearMarks(document);
        collect(document);
        window.__resilientControls = elements;
        return JSON.stringify(meta);
        """;
}
