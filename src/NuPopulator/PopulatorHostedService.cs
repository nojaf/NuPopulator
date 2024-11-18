using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NuPopulator;

public class PopulatorHostedService(
    ILogger<PopulatorHostedService> logger,
    Options options,
    IHostApplicationLifetime hostApplicationLifetime
) : IHostedService
{
    private const int NumberOfTypes = 5;

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

        foreach (var project in projects)
        {
            if (project.IsFSharpProject)
            {
                await GenerateFSharpCode.Generate(NumberOfTypes, project);
            }
            else
            {
                await GenerateCSharpCode.Generate(NumberOfTypes, project);
            }
        }

        hostApplicationLifetime.StopApplication();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
