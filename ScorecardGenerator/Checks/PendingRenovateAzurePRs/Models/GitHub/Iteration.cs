// ReSharper disable InconsistentNaming - Used for deserialization
// ReSharper disable NotAccessedPositionalProperty.Global - Used for deserialization
// ReSharper disable ClassNeverInstantiated.Global - Used for deserialization
namespace ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models.GitHub;

public record Iteration(
    IterationValue[] value,
    int count
);

public record IterationValue(
    int id
);