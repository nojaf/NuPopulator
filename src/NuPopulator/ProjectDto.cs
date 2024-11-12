namespace NuPopulator;

using System.Collections.Generic;

public class ProjectDto
{
    public string ProjectName { get; set; } = "";
    public string Sdk { get; set; } = "";
    public Dictionary<string, string> Properties { get; set; } = new(0);
    public Dictionary<
        string /*condition*/
        ,
        List<string>
    > ProjectReferences { get; set; } = new(0);
    public Dictionary<
        string /*condition*/
        ,
        Dictionary<string, string>
    > PackageReferences { get; set; } = new(0);
}
