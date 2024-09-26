using System.Collections.Immutable;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Models;

namespace ScorecardGenerator.Calculation;

public record RunInfo(IDictionary<string, IList<CheckInfo>> Checks, ImmutableSortedDictionary<string, RunInfo.ServiceScorecard> ServiceScores)
{
    public record ServiceScorecard
    (
        Dictionary<string, IList<BaseCheck.Deduction>> DeductionsByCheck,
        int Average
    );
}