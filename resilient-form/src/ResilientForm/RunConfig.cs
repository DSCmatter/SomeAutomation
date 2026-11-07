using System.Text.Json;

namespace ResilientForm;

public sealed record RunConfig(string Url, bool Headless, IReadOnlyList<FieldIntent> Fields)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static RunConfig Load(string[] args)
    {
        string? url = null;
        string? configPath = null;
        var headless = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--url":
                    url = Next(args, ref i, arg);
                    break;
                case "--config":
                    configPath = Next(args, ref i, arg);
                    break;
                case "--headless":
                    headless = true;
                    break;
                case "--help":
                case "-h":
                    Console.WriteLine(HelpText);
                    Environment.Exit(0);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument \"{arg}\". {HelpText}");
            }
        }

        var path = ResolveConfigPath(configPath);
        var file = JsonSerializer.Deserialize<ConfigFile>(File.ReadAllText(path), JsonOptions)
            ?? throw new FormatException($"Config file {path} was empty.");
        if (file.Fields == null || file.Fields.Count == 0)
        {
            throw new FormatException($"Config file {path} needs a fields array.");
        }

        var fields = file.Fields.Select(ParseField).ToList();
        var resolvedUrl = url ?? file.Url;
        if (string.IsNullOrWhiteSpace(resolvedUrl))
        {
            throw new ArgumentException("Pass --url or set url in the config file.");
        }

        return new RunConfig(resolvedUrl.Trim(), headless, fields);
    }

    private static FieldIntent ParseField(ConfigField field)
    {
        if (string.IsNullOrWhiteSpace(field.Label) || string.IsNullOrWhiteSpace(field.Value))
        {
            throw new FormatException("Each field needs a label and a value.");
        }

        var kind = (field.Kind ?? "").Trim().ToLowerInvariant() switch
        {
            "text" => ControlKind.Text,
            "radio" => ControlKind.Radio,
            "select" => ControlKind.Select,
            _ => throw new FormatException($"Unknown field kind \"{field.Kind}\". Use text, radio, or select."),
        };
        return new FieldIntent(field.Label.Trim(), kind, field.Value.Trim());
    }

    private static string ResolveConfigPath(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            if (!File.Exists(requested))
            {
                throw new FileNotFoundException($"Config file not found: {requested}", requested);
            }

            return requested;
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "fields.json"),
            Path.Combine(AppContext.BaseDirectory, "fields.json"),
        };
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not find fields.json. Pass --config.");
    }

    private static string Next(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value after {flag}.");
        }

        index++;
        return args[index];
    }

    private const string HelpText = """
        Usage: ResilientForm --url <page> [--config fields.json] [--headless]
        The config file lists the label, kind, and value for each field.
        --url replaces the url in the config file.
        """;

    private sealed class ConfigFile
    {
        public string? Url { get; set; }

        public List<ConfigField>? Fields { get; set; }
    }

    private sealed class ConfigField
    {
        public string? Label { get; set; }

        public string? Kind { get; set; }

        public string? Value { get; set; }
    }
}
