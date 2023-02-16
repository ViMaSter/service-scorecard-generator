using ScorecardGenerator.Checks;

namespace ScorecardGenerator.Test.Helper;

internal static class DeductionHelper
{
    public static void CountAndFinalScore(this IList<BaseCheck.Deduction> deductions, int expectedCount, int? expectedFinalScore)
    {   
        if (deductions.Any())
        {
            TestContext.WriteLine($"Occured deductions: {Environment.NewLine}{string.Join(Environment.NewLine, deductions)}");
        }        
        Assert.That(deductions, Has.Count.EqualTo(expectedCount));
        Assert.That(deductions.CalculateFinalScore(), Is.EqualTo(expectedFinalScore));
    }
}