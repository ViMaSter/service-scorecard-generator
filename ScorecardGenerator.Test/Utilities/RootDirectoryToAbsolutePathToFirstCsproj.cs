namespace ScorecardGenerator.Test.Utilities;

public class RootDirectoryToAbsolutePathToFirstCsproj
{
    [TestCase]
    public void WorksWithTwoCsProjFiles()
    {
        var serviceRootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(serviceRootDirectory);
        File.WriteAllText(Path.Combine(serviceRootDirectory, "test1.csproj"), "test");
        File.WriteAllText(Path.Combine(serviceRootDirectory, "test2.csproj"), "test");
        Assert.That(ScorecardGenerator.Utilities.RootDirectoryToAbsolutePathToFirstCsproj(serviceRootDirectory), Is.EqualTo(Path.Join(serviceRootDirectory, "test1.csproj")));
        Directory.Delete(serviceRootDirectory, true);
    }
    
    [TestCase]
    public void ThrowsWithEmptyDirectory()
    {
        var serviceRootDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(serviceRootDirectory);
        Assert.Throws<FileNotFoundException>(() => ScorecardGenerator.Utilities.RootDirectoryToAbsolutePathToFirstCsproj(serviceRootDirectory));
        Directory.Delete(serviceRootDirectory, true);   
    }
}