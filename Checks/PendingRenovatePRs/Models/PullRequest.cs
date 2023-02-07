// ReSharper disable InconsistentNaming - Used for deserialization
// ReSharper disable NotAccessedPositionalProperty.Global - Used for deserialization
// ReSharper disable ClassNeverInstantiated.Global - Used for deserialization
namespace ScorecardGenerator.Checks.PendingRenovatePRs.Models;

public record PullRequest(
    PullRequestValue[] value,
    int count
);

public record PullRequestValue(
    Repository repository,
    int pullRequestId,
    string sourceRefName
);

public record Repository(
    string id,
    string name
);