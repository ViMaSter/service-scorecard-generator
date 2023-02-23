using System.Reflection;

namespace ScorecardGenerator.Test.Visualizer;

public abstract class TestWithNeighboringDirectoryFixture
{
    protected string WorkingDirectory { get; private set; } = "";
    protected string RelativePathToServiceRoot { get; private set; } = "";
    private string TargetWorkingDirectory { get; set; } = "";

    [SetUp]
    public void BeforeEach()
    {
        var resourceName = GetType().FullName!;
        var assembly = Assembly.GetExecutingAssembly();
        var matchingResources = assembly.GetManifestResourceNames().Where(resource => resource.StartsWith(resourceName)).ToList();
        WorkingDirectory = Path.GetTempPath();
        RelativePathToServiceRoot = Guid.NewGuid().ToString();
        var tempDirectory = Path.Join(WorkingDirectory, RelativePathToServiceRoot);
        Directory.CreateDirectory(tempDirectory);
        foreach (var matchingResource in matchingResources)
        {
            var resourceStream = assembly.GetManifestResourceStream(matchingResource);
            var parts = matchingResource.Replace(resourceName, "").Trim('.').Split(".").Select(part=>part.Replace("_", "-")).ToList();
            var directory = string.Join(Path.DirectorySeparatorChar, parts.SkipLast(2).Prepend(tempDirectory));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            var pats = Path.Join(directory, string.Join(".", parts.TakeLast(2)));
            using var fileStream = File.Create(pats);
            resourceStream!.CopyTo(fileStream);
        }
        TargetWorkingDirectory = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(TargetWorkingDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(Path.Join(WorkingDirectory, RelativePathToServiceRoot), true);
        Directory.Delete(TargetWorkingDirectory, true);
    }
}