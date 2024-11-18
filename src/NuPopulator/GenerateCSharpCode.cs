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

    private static Task ProduceTypes(int numberOfTypes, ProjectInfo projectInfo)
    {
        var typeDefs = String.Join(
            "\n",
            Enumerable
                .Range(1, numberOfTypes)
                .Select(i => $"\tpublic class {MapIntToAlphabet(i)} {{ }}")
        );
        var content = $"namespace {projectInfo.ProjectName} {{\n{typeDefs}\n}}";
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Produce.cs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    private static Task ConsumeReferenceProjectTypes(ProjectInfo projectInfo)
    {
        var types = String.Join(
            ", ",
            projectInfo.ReferencedProjects.Select((x, idx) => $"{x.ProjectName}.A p{idx}")
        );
        var staticMethod = $"public static A Function({types}) {{ return default(A); }}";
        var content =
            $"namespace {projectInfo.ProjectName} {{\n\tpublic static class Consume {{\n\t\t{staticMethod}\n\t}}\n}}";
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Consume.cs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    public static async Task Generate(int numberOfTypes, ProjectInfo projectInfo)
    {
        PurgeExistingFiles(projectInfo);
        await ProduceTypes(numberOfTypes, projectInfo);
        await ConsumeReferenceProjectTypes(projectInfo);
    }
}
