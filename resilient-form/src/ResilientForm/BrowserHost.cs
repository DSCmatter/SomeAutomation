using OpenQA.Selenium.Edge;

namespace ResilientForm;

public static class BrowserHost
{
    public static EdgeDriver Open(bool headless)
    {
        var options = new EdgeOptions();
        if (headless)
        {
            options.AddArgument("--headless=new");
        }

        options.AddArgument("--window-size=1400,1000");
        var service = EdgeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        var driver = new EdgeDriver(service, options);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
        driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(30);
        return driver;
    }
}
