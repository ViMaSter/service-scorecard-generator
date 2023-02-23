using System.Reflection;

namespace ScorecardGenerator.Test.Checks;

public abstract class TestWithNeighboringCsprojFixture
{
    private string WorkingDirectory { get; set; } = "";
    private string RelativePathToProjectFile { get; set; } = "";
    protected string AbsolutePathToProjectFile => Path.Join(WorkingDirectory, RelativePathToProjectFile);

    [SetUp]
    public void Setup()
    {
        var resourceName = GetType().FullName;
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream(resourceName+".csproj.xml");
        
        WorkingDirectory = Path.GetTempPath();
        var relativePathToServiceRoot = Guid.NewGuid().ToString();
        var tempDirectory = Path.Join(WorkingDirectory, relativePathToServiceRoot);
        RelativePathToProjectFile = Path.Join(relativePathToServiceRoot, resourceName + ".csproj");
        Directory.CreateDirectory(tempDirectory);
        using var fileStream = File.Create(AbsolutePathToProjectFile);
        resourceStream!.CopyTo(fileStream);
    }

}