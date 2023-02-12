namespace ScorecardGenerator.Test.Utilities;

public class GetNameFromCheckClass
{
    [TestCase]
    public void WorksForLatestNET()
    {
        var check = new ScorecardGenerator.Checks.LatestNET.Check(null);
        var name = ScorecardGenerator.Utilities.GetNameFromCheckClass(check);
        Assert.That(name, Is.EqualTo(nameof(ScorecardGenerator.Checks.LatestNET)));
    }
}