namespace ScorecardGenerator;

internal abstract class Utilities
{
    public static bool ContainsCsProj(string directory)
    {
        return Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any();
    }

    public static string RootDirectoryToProjectNameFromCsproj(string serviceRootDirectory)
    { 
        var csprojFiles = Directory.GetFiles(serviceRootDirectory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            throw new FileNotFoundException("No csproj found to determine project name");
        }

        return Path.GetFileName(csprojFiles.First());
    }
}