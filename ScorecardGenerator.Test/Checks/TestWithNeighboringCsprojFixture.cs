using System.Reflection;

namespace ScorecardGenerator.Test.Checks;

public abstract class TestWithNeighboringCsprojFixture
{
    protected string WorkingDirectory { get; private set; } = "";
    protected string RelativePathToServiceRoot { get; private set; } = "";

    [SetUp]
    public void Setup()
    {
        var resourceName = GetType().FullName;
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream(resourceName+".csproj.xml");
        
        WorkingDirectory = Path.GetTempPath();
        RelativePathToServiceRoot = Guid.NewGuid().ToString();
        var tempDirectory = Path.Join(WorkingDirectory, RelativePathToServiceRoot);
        Directory.CreateDirectory(tempDirectory);
        var tempFile = Path.Join(tempDirectory, resourceName+".csproj");
        using var fileStream = File.Create(tempFile);
        resourceStream!.CopyTo(fileStream);
    }

}