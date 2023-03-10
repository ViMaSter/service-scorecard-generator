using ScorecardGenerator.Checks;

namespace ScorecardGenerator;

public static class DeductionExtensions
{
    public static int? CalculateFinalScore(this IList<BaseCheck.Deduction> deductions)
    {
        if (deductions.Any(deduction => deduction.IsDisqualification))
        {
            return null;
        }
        var scoreAfterDeductions = deductions.Aggregate(100, (current, deduction) => current - deduction.Score!.Value);

        return Math.Max(0, scoreAfterDeductions);
    }
}

public abstract class Utilities
{
    public static CheckInfo GenerateCheckRunInfo(BaseCheck check)
    {
        return new CheckInfo(GetNameFromCheckClass(check), check.InfoPageContent);
    }
    
    public static string GetNameFromCheckClass(BaseCheck check)
    {
        return check.GetType().FullName!.Split(".")[^2];
    }
}