namespace ScorecardGenerator.Calculation;

public record RunInfo(IEnumerable<string> checks, Dictionary<string, Dictionary<string, int>> serviceScores);