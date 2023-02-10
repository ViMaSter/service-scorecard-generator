using System.Collections.Immutable;

namespace ScorecardGenerator.Calculation;

public record RunInfo(IDictionary<string, IList<CheckInfo>> Checks, ImmutableSortedDictionary<string, RunInfo.ServiceScorecard> ServiceScores)
{
    public record ServiceScorecard
    (
        Dictionary<string, int> ScoreByCheck,
        int Average
    );
}