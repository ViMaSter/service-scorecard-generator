using System.Collections.Immutable;

namespace ScorecardGenerator.Calculation;

public record RunInfo(IDictionary<string, IList<string>> Checks, ImmutableSortedDictionary<string, RunInfo.ServiceScorecard> ServiceScores)
{
    public record ServiceScorecard
    (
        Dictionary<string, int> scorebyCheck,
        int Average
    );
}