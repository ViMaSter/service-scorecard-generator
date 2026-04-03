using System.Reflection;

namespace ScorecardGenerator.Test.Checks.ProperDockerfile;

public class TestWithNeighboringCsprojAndDockerfileFixture : TestWithNeighboringCsprojFixture
{
    [SetUp]
    public void SetupWithDockerfile()
    {
        Setup();
        
        var resourceName = GetType().FullName;
        var assembly = Assembly.GetExecutingAssembly();
        var resourceStream = assembly.GetManifestResourceStream(resourceName+".Dockerfile");
        
        var absolutePathToProjectDirectory = Path.GetDirectoryName(AbsolutePathToProjectFile)!;
        Directory.CreateDirectory(absolutePathToProjectDirectory);
        var tempFile = Path.Join(absolutePathToProjectDirectory, "Dockerfile");
        using var fileStream = File.Create(tempFile);
        resourceStream!.CopyTo(fileStream);
    }
}