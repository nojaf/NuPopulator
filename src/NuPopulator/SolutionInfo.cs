using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NuPopulator;

public record ProjectInfo(FileInfo ProjectFile, ProjectInfo[] ReferencedProjects)
{
    public string ProjectName => Path.GetFileNameWithoutExtension(ProjectFile.Name);
    public bool IsFSharpProject = ProjectFile.Name.EndsWith(
        ".fsproj",
        StringComparison.OrdinalIgnoreCase
    );
}

public class SolutionInfo
{
    public static async Task<ProjectInfo[]> LoadFromJsonFile(
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

        var solutionRoot = Path.Combine(Path.GetDirectoryName(jsonFilePath), "solution");
        if (!Directory.Exists(solutionRoot))
        {
            throw new DirectoryNotFoundException(solutionRoot);
        }

        var projectInfos = new List<ProjectInfo>(projects.Length);

        foreach (var project in projects)
        {
            var projectFolder = Path.Combine(
                solutionRoot,
                Path.GetFileNameWithoutExtension(project.ProjectName)
            );
            if (!Directory.Exists(projectFolder))
            {
                throw new DirectoryNotFoundException(projectFolder);
            }

            var projectFile = Path.Combine(projectFolder, project.ProjectName);
            if (!File.Exists(projectFile))
            {
                throw new FileNotFoundException(projectFile);
            }

            projectInfos.Add(
                new ProjectInfo(new FileInfo(projectFile), Array.Empty<ProjectInfo>())
            );
        }

        for (var idx = 0; idx < projectInfos.Count; idx++)
        {
            var projectInfo = projectInfos[idx];
            var project = projects.Single(p => p.ProjectName == projectInfo.ProjectFile.Name);
            var projectReferences = project.ProjectReferences.TryGetValue("", out var reference)
                ? reference
                : Enumerable.Empty<string>();
            var referencedProjects = projectReferences
                .Select(pr => projectInfos.Single(pi => pr == pi.ProjectFile.Name))
                .ToArray();
            projectInfos[idx] = projectInfo with { ReferencedProjects = referencedProjects };
        }

        return projectInfos.ToArray();
    }
}
