namespace ScorecardGenerator;

public record Iteration(
    Iteration.Value[] value,
    int count
)
{
    

    public record Value(
        int id,
        string description,
        Author author,
        string createdDate,
        string updatedDate,
        SourceRefCommit sourceRefCommit,
        TargetRefCommit targetRefCommit,
        CommonRefCommit commonRefCommit,
        bool hasMoreCommits,
        string reason
    );

    public record Author(
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

    public record SourceRefCommit(
        string commitId
    );

    public record TargetRefCommit(
        string commitId
    );

    public record CommonRefCommit(
        string commitId
    );
}