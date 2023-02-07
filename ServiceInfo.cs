namespace ScorecardGenerator;

public record ServiceInfo(Dictionary<string, int> ScoreByCheckName, int Average)
{
    public const int GoldWeight = 10;
    public const int SilverWeight = 5;
    public const int BronzeWeight = 1;
}