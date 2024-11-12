using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuPopulator;

public class Program
{
    private static LogLevel ParseLogLevel(string level)
    {
        if (String.IsNullOrWhiteSpace(level))
        {
            return LogLevel.Information;
        }
        // Mapping shorthand values to full enum names
        var shorthandMappings = new (string shorthand, LogLevel level)[]
        {
            ("trace", LogLevel.Trace),
            ("debug", LogLevel.Debug),
            ("info", LogLevel.Information),
            ("warn", LogLevel.Warning),
            ("error", LogLevel.Error),
            ("critical", LogLevel.Critical),
        };

        // Try matching the shorthand or full enum name, case-insensitive
        foreach (var (shorthand, logLevel) in shorthandMappings)
        {
            if (string.Equals(level, shorthand, StringComparison.OrdinalIgnoreCase))
            {
                return logLevel;
            }
        }

        // Fallback to try parsing the full name, otherwise default to Information
        if (Enum.TryParse(level, true, out LogLevel parsedLevel))
        {
            return parsedLevel;
        }

        Console.WriteLine($"Invalid log level: {level}. Defaulting to Information.");
        return LogLevel.Information;
    }

    public static async Task Main(string[] args)
    {
        // Parse command-line options
        var result = Parser.Default.ParseArguments<Options>(args);

        // Handle the parsed options
        await result.WithParsedAsync(async options =>
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    var logLevel = ParseLogLevel(options.LogLevel);
                    logging.ClearProviders(); // Optionally clear all providers if you only want custom logging
                    logging
                        .AddSimpleConsole(consoleOptions =>
                        {
                            consoleOptions.ColorBehavior = Microsoft
                                .Extensions
                                .Logging
                                .Console
                                .LoggerColorBehavior
                                .Enabled;
                            consoleOptions.SingleLine = true;
                            consoleOptions.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                        })
                        .SetMinimumLevel(logLevel);

                    logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(options);
                    services.AddHostedService<PopulatorHostedService>();
                })
                .Build()
                .RunAsync();
        });
    }
}
