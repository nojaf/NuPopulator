namespace NuPopulator;

public class GenerateCSharpCode
{
    private static void PurgeExistingFiles(ProjectInfo projectInfo)
    {
        Directory
            .EnumerateFiles(
                projectInfo.ProjectFile.DirectoryName,
                "*.cs",
                SearchOption.AllDirectories
            )
            .ToList()
            .ForEach(File.Delete);
    }

    private static string MapIntToAlphabet(int value)
    {
        return value >= 1 && value <= 26 ? ((char)('A' + value - 1)).ToString() : "Unknown";
    }

    private static Task ProduceTypes(IEnumerable<string> typeNames, ProjectInfo projectInfo)
    {
        var content = RoslynCodeGen.MkType(typeNames, projectInfo.ProjectName);
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Produce.cs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    private static Task ConsumeReferenceProjectTypes(
        IEnumerable<string> typeNames,
        ProjectInfo projectInfo
    )
    {
        var content = RoslynCodeGen.MkConsume(
            typeNames,
            projectInfo.ProjectName,
            projectInfo.ReferencedProjects.Select(rp => rp.ProjectName)
        );
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Consume.cs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    public static async Task Generate(IEnumerable<string> typeNames, ProjectInfo projectInfo)
    {
        PurgeExistingFiles(projectInfo);
        await ProduceTypes(typeNames, projectInfo);
        await ConsumeReferenceProjectTypes(typeNames, projectInfo);
    }
}
