using OpenQA.Selenium;

namespace ResilientForm;

public sealed record FieldResult(string Label, string Kind, bool Passed, string Detail);

public sealed record RunReport(string Url, IReadOnlyList<FieldResult> Fields)
{
    public bool Passed => Fields.Count > 0 && Fields.All(field => field.Passed);

    public string Format()
    {
        var lines = new List<string> { $"url: {Url}" };
        foreach (var field in Fields)
        {
            var status = field.Passed ? "PASS" : "FAIL";
            lines.Add($"{status} {field.Label} ({field.Kind}): {field.Detail}");
        }

        lines.Add(Passed ? "PASS" : "FAIL");
        return string.Join(Environment.NewLine, lines);
    }
}

public sealed class FormRunner
{
    private readonly IWebDriver _driver;

    public FormRunner(IWebDriver driver)
    {
        _driver = driver;
    }

    public RunReport Run(RunConfig config)
    {
        _driver.Navigate().GoToUrl(config.Url);
        var results = new List<FieldResult>();
        foreach (var intent in config.Fields)
        {
            results.Add(RunField(intent));
        }

        if (results.Any(result => !result.Passed))
        {
            SaveFailureShot();
        }

        return new RunReport(config.Url, results);
    }

    private FieldResult RunField(FieldIntent intent)
    {
        var kind = KindName(intent.Kind);
        try
        {
            var finder = new FieldFinder(_driver);
            var located = finder.Require(intent);
            var detail = intent.Kind switch
            {
                ControlKind.Text => TypeText(located, intent.Value),
                ControlKind.Radio => ChooseRadio(located, intent.Value),
                ControlKind.Select => ChooseOption(located, intent.Value),
                _ => throw Unhandled(intent.Kind),
            };
            return new FieldResult(intent.Label, kind, true, $"{detail} caption \"{located.Caption}\" score {located.Score}");
        }
        catch (Exception ex) when (ex is FieldNotFoundException or WebDriverException or InvalidOperationException)
        {
            return new FieldResult(intent.Label, kind, false, ex.Message);
        }
    }

    private string TypeText(LocatedField located, string value)
    {
        var element = located.Element;
        ScrollIntoView(element);
        element.Click();
        element.SendKeys(Keys.Control + "a");
        element.SendKeys(Keys.Backspace);
        element.SendKeys(value);
        var actual = element.GetAttribute("value") ?? "";
        if (!string.Equals(actual, value, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Typed \"{value}\" but the field read back \"{actual}\".");
        }

        return $"typed \"{value}\"";
    }

    private string ChooseRadio(LocatedField located, string value)
    {
        var element = located.Element;
        ScrollIntoView(element);
        try
        {
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }

        if (!element.Selected)
        {
            throw new InvalidOperationException($"Clicked \"{value}\" but the radio is not selected.");
        }

        return $"selected \"{value}\"";
    }

    private string ChooseOption(LocatedField located, string value)
    {
        var select = located.Element;
        ScrollIntoView(select);
        var options = select.FindElements(By.TagName("option"));
        var bestIndex = -1;
        var bestScore = 0;
        var bestText = "";
        for (var i = 0; i < options.Count; i++)
        {
            var text = options[i].Text ?? "";
            var optionValue = options[i].GetAttribute("value") ?? "";
            var score = Math.Max(TextMatch.Score(value, text), TextMatch.Score(value, optionValue));
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
                bestText = text.Trim();
            }
        }

        if (bestIndex < 0 || bestScore < FieldFinder.AcceptScore)
        {
            throw new FieldNotFoundException($"Dropdown has no option matching \"{value}\".");
        }

        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].selectedIndex = arguments[1]; arguments[0].dispatchEvent(new Event('change', { bubbles: true }));",
            select,
            bestIndex);
        var selected = ((IJavaScriptExecutor)_driver).ExecuteScript(
            "const node = arguments[0]; return node.options[node.selectedIndex] ? node.options[node.selectedIndex].text : '';",
            select) as string ?? "";
        if (TextMatch.Score(value, selected) < FieldFinder.AcceptScore)
        {
            throw new InvalidOperationException($"Chose \"{value}\" but the dropdown read back \"{selected.Trim()}\".");
        }

        return $"selected \"{bestText}\"";
    }

    private void ScrollIntoView(IWebElement element)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript(
            "arguments[0].scrollIntoView({ block: 'center', inline: 'nearest' });",
            element);
    }

    private void SaveFailureShot()
    {
        if (_driver is not ITakesScreenshot camera)
        {
            return;
        }

        Directory.CreateDirectory("artifacts");
        var path = Path.Combine("artifacts", "failure.png");
        camera.GetScreenshot().SaveAsFile(path);
    }

    private static string KindName(ControlKind kind)
    {
        return kind switch
        {
            ControlKind.Text => "text",
            ControlKind.Radio => "radio",
            ControlKind.Select => "select",
            _ => throw Unhandled(kind),
        };
    }

    private static Exception Unhandled(ControlKind kind)
    {
        return new ArgumentOutOfRangeException(nameof(kind), kind, "Unhandled control kind.");
    }
}
