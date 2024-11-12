using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NuPopulator;

public class SolutionInfo
{
    public static async Task<ProjectDto[]> LoadFromJsonFile(
        ILogger logger,
        CancellationToken ct,
        string jsonFilePath
    )
    {
        if (!File.Exists(jsonFilePath))
        {
            logger.LogCritical("File {path} does not exist", jsonFilePath);
            return [];
        }

        var text = await File.ReadAllTextAsync(jsonFilePath, ct);
        var projects = JsonSerializer.Deserialize<ProjectDto[]>(text);

        if (projects == null || projects.Length == 0)
        {
            logger.LogCritical("File {path} does not contain any projects", jsonFilePath);
            return [];
        }

        return projects;
    }
}
