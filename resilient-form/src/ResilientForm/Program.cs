namespace ResilientForm;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var config = RunConfig.Load(args);
            using var driver = BrowserHost.Open(config.Headless);
            var report = new FormRunner(driver).Run(config);
            Console.WriteLine(report.Format());
            return report.Passed ? 0 : 1;
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException or FileNotFoundException)
        {
            Console.Error.WriteLine(ex.Message);
            return 2;
        }
    }
}
