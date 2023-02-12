using ScorecardGenerator.Checks;

namespace ScorecardGenerator;

public static class DeductionExtensions
{
    public static int CalculateFinalScore(this IList<BaseCheck.Deduction> deductions)
    {
        var scoreAfterDeductions = deductions.Aggregate(100, (current, deduction) => current - deduction.Score);

        return Math.Max(0, scoreAfterDeductions);
    }
}

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

        return csprojFiles.First();
    }

    public static string GetNameFromCheckClass(BaseCheck check)
    {
        return check.GetType().FullName!.Split(".")[^2];
    }
}