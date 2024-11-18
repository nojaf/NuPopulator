using System.Xml.Linq;

namespace NuPopulator;

public static class GenerateFSharpCode
{
    private static string MapIntToAlphabet(int value)
    {
        return value >= 1 && value <= 26 ? ((char)('A' + value - 1)).ToString() : "Unknown";
    }

    private static Task ProduceTypes(int numberOfTypes, ProjectInfo projectInfo)
    {
        var content = NuPopulator.FSharp.Generate.mkProducer(projectInfo.ProjectName);
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Produce.fs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    private static Task ConsumeReferenceProjectTypes(ProjectInfo projectInfo)
    {
        var content = NuPopulator.FSharp.Generate.mkConsumer(
            projectInfo.ProjectName,
            projectInfo.ReferencedProjects.Select(r => r.ProjectName)
        );
        var outputPath = Path.Combine(projectInfo.ProjectFile.DirectoryName, "Consume.fs");
        return File.WriteAllTextAsync(outputPath, content);
    }

    private static async Task<XDocument> LoadXmlAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        return await XDocument.LoadAsync(stream, LoadOptions.None, default);
    }

    private static async Task SaveXmlAsync(XDocument doc, string path)
    {
        await using var stream = File.Create(path);
        await doc.SaveAsync(stream, SaveOptions.None, default);
    }

    private static async Task UpdateProjectFile(ProjectInfo projectInfo)
    {
        var doc = await LoadXmlAsync(projectInfo.ProjectFile.FullName);

        // Find and remove all <Compile> elements with an Include attribute
        doc.Descendants("Compile")
            .Where(e => e.Attribute("Include") != null)
            .ToList()
            .ForEach(e => e.Remove());

        // Find an empty <ItemGroup> under <Project> or add a new one before </Project>
        var root = doc.Root;
        if (root == null || root.Name != "Project")
        {
            throw new InvalidOperationException("The root element is not <Project>.");
        }

        // Check for an existing empty <ItemGroup>
        var emptyItemGroup = root.Elements("ItemGroup").FirstOrDefault(e => !e.Elements().Any());

        // If no empty <ItemGroup> is found, add a new one
        if (emptyItemGroup == null)
        {
            emptyItemGroup = new XElement("ItemGroup");
            root.Add(emptyItemGroup); // Adds it at the end, right before </Project>
        }

        // Add the two <Compile> elements
        emptyItemGroup.Add(
            new XElement("Compile", new XAttribute("Include", "Produce.fs")),
            new XElement("Compile", new XAttribute("Include", "Consume.fs"))
        );

        // Save the modified document
        await SaveXmlAsync(doc, projectInfo.ProjectFile.FullName);
    }

    public static async Task Generate(int numberOfTypes, ProjectInfo projectInfo)
    {
        await ProduceTypes(numberOfTypes, projectInfo);
        await ConsumeReferenceProjectTypes(projectInfo);
        await UpdateProjectFile(projectInfo);
    }
}
