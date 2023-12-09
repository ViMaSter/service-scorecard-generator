// ReSharper disable InconsistentNaming - Used for deserialization
// ReSharper disable NotAccessedPositionalProperty.Global - Used for deserialization
// ReSharper disable ClassNeverInstantiated.Global - Used for deserialization
namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades.Models.GitHub;

public record Changes(
    ChangeEntries[] changeEntries
);

public record ChangeEntries(
    Item item
);

public record Item(
    string objectId,
    string path
);