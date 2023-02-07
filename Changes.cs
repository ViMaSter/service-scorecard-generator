namespace ScorecardGenerator;

public record Changes(
    Changes.ChangeEntries[] changeEntries)
{
    public record ChangeEntries(
        int changeTrackingId,
        int changeId,
        Item item,
        string changeType
    );

    public record Item(
        string objectId,
        string path
    );
}