using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NuPopulator;

public class PopulatorHostedService(
    ILogger<PopulatorHostedService> logger,
    Options options,
    IHostApplicationLifetime hostApplicationLifetime
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("About to process {path}", options.JsonPath);
        var projects = await SolutionInfo.LoadFromJsonFile(
            logger,
            cancellationToken,
            options.JsonPath
        );
        logger.LogInformation(
            "Found {length} projects in {fullPath}.",
            projects.Length,
            Path.GetFullPath(options.JsonPath)
        );
        hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
