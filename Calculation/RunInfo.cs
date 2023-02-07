namespace ScorecardGenerator.Calculation;

public record RunInfo(IDictionary<string, IList<string>> Checks, IDictionary<string, ServiceInfo> ServiceScores);