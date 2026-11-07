using OpenQA.Selenium;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ResilientForm.Tests;

public class FinderTests
{
    [Fact]
    public void Fills_the_visible_fields_when_ids_order_and_tags_change()
    {
        using var driver = BrowserHost.Open(headless: true);
        var path = Path.Combine(Path.GetTempPath(), $"resilient-form-{Guid.NewGuid():N}.html");
        File.WriteAllText(path, MutatedPage);
        try
        {
            driver.Navigate().GoToUrl(new Uri(path).AbsoluteUri);
            var runner = new FormRunner(driver);
            var report = runner.Run(new RunConfig(new Uri(path).AbsoluteUri, true, Intents()));

            Assert.True(report.Passed, report.Format());
            var values = ((IJavaScriptExecutor)driver).ExecuteScript(
                """
                const section = document.querySelector('section');
                const texts = [...section.querySelectorAll('input[type="text"]')].map((node) => node.value);
                const radios = [...section.querySelectorAll('input[type="radio"]')].map((node) => node.checked);
                const selected = section.querySelector('select').selectedOptions[0].text;
                const hidden = document.querySelector('form[hidden] input[type="text"]').value;
                const shadow = document.querySelector('decoy-name').shadowRoot.querySelector('input').value;
                return [texts[0], texts[1], radios[0], radios[1], selected.trim(), hidden, shadow];
                """) as System.Collections.IEnumerable;

            Assert.NotNull(values);
            var list = values.Cast<object>().ToList();
            Assert.Equal("", list[0]);
            Assert.Equal("Ada", list[1]);
            Assert.Equal(false, list[2]);
            Assert.Equal(true, list[3]);
            Assert.Equal("India", list[4]);
            Assert.Equal("", list[5]);
            Assert.Equal("", list[6]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Finds_a_field_that_exists_only_inside_open_shadow_dom()
    {
        using var driver = BrowserHost.Open(headless: true);
        var path = Path.Combine(Path.GetTempPath(), $"resilient-shadow-{Guid.NewGuid():N}.html");
        File.WriteAllText(path, ShadowOnlyPage);
        try
        {
            driver.Navigate().GoToUrl(new Uri(path).AbsoluteUri);
            var located = new FieldFinder(driver).Require(new FieldIntent("First Name", ControlKind.Text, "Ada"));
            located.Element.SendKeys("Ada");
            var actual = ((IJavaScriptExecutor)driver).ExecuteScript(
                "return document.querySelector('only-name').shadowRoot.querySelector('input').value;") as string;
            Assert.Equal("Ada", actual);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IReadOnlyList<FieldIntent> Intents()
    {
        return
        [
            new FieldIntent("First Name", ControlKind.Text, "Ada"),
            new FieldIntent("Gender", ControlKind.Radio, "Female"),
            new FieldIntent("State", ControlKind.Select, "India"),
        ];
    }

    private const string MutatedPage = """
        <!DOCTYPE html>
        <html>
        <body>
          <form hidden>
            <label for="fname">First Name</label>
            <input id="fname" name="First Name" class="form-control" type="text">
            <label>Gender</label>
            <input type="radio" name="gender" value="Female"><span>Female</span>
            <label for="state">State</label>
            <select id="state" name="State"><option>India</option></select>
          </form>
          <section>
            <div>
              <select>
                <option>Choose</option>
                <option>Japan</option>
                <option>India</option>
              </select>
              <b>State</b>
            </div>
            <div>
              <p>Gender</p>
              <div>
                <input type="radio" value="m"><em>Male</em>
                <input type="radio" value="f"><em>Female</em>
              </div>
            </div>
            <div>
              <span>Last Name</span>
              <input type="text">
            </div>
            <div>
              <div><i>First Name</i></div>
              <input type="text">
            </div>
          </section>
          <decoy-name></decoy-name>
          <script>
            customElements.define('decoy-name', class extends HTMLElement {
              constructor() {
                super();
                this.attachShadow({ mode: 'open' }).innerHTML =
                  '<label>First Name</label><input type="text">';
              }
            });
          </script>
        </body>
        </html>
        """;

    private const string ShadowOnlyPage = """
        <!DOCTYPE html>
        <html>
        <body>
          <only-name></only-name>
          <script>
            customElements.define('only-name', class extends HTMLElement {
              constructor() {
                super();
                this.attachShadow({ mode: 'open' }).innerHTML =
                  '<span>First Name</span><input type="text">';
              }
            });
          </script>
        </body>
        </html>
        """;
}
