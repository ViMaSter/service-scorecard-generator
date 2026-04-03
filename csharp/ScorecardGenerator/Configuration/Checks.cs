using ScorecardGenerator.Checks;

namespace ScorecardGenerator.Configuration;

public record Checks    
(
    List<BaseCheck> Gold, List<BaseCheck> Silver, List<BaseCheck> Bronze
)
{
    public const int GOLD_WEIGHT = 10;
    public const int SILVER_WEIGHT = 5;
    public const int BRONZE_WEIGHT = 1;
}