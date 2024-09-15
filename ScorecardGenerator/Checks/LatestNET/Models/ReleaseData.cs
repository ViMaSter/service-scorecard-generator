using Newtonsoft.Json;

namespace ScorecardGenerator.Checks.LatestNET.Models;

public record ReleaseData(
    [JsonProperty("releases-index")]
    Release[] ReleasesIndex
);