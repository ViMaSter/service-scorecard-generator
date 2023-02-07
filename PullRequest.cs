namespace ScorecardGenerator;

public record PullRequest(
    PullRequest.Value[] value,
    int count
)
{
public record Value(
    Repository repository,
    int pullRequestId,
    int codeReviewId,
    string status,
    CreatedBy createdBy,
    string creationDate,
    string title,
    string sourceRefName,
    string targetRefName,
    string mergeStatus,
    bool isDraft,
    string mergeId,
    LastMergeSourceCommit lastMergeSourceCommit,
    LastMergeTargetCommit lastMergeTargetCommit,
    LastMergeCommit lastMergeCommit,
    object[] reviewers,
    string url,
    bool supportsIterations
);

public record Repository(
    string id,
    string name,
    string url,
    Project project
);

public record Project(
    string id,
    string name,
    string state,
    string visibility,
    string lastUpdateTime
);

public record CreatedBy(
    string displayName,
    string url,
    _links _links,
    string id,
    string uniqueName,
    string imageUrl,
    string descriptor
);

public record _links(
    Avatar avatar
);

public record Avatar(
    string href
);

public record LastMergeSourceCommit(
    string commitId,
    string url
);

public record LastMergeTargetCommit(
    string commitId,
    string url
);

public record LastMergeCommit(
    string commitId,
    string url
);
}