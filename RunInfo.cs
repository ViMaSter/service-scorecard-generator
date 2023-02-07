namespace ScorecardGenerator.Calculation;

public record RunInfo(IDictionary<string, IList<string>> checks, IDictionary<string, ServiceInfo> serviceScores);