using Newtonsoft.Json;

namespace ScorecardGenerator.Checks.LatestNET.Models;

public record Release(
    [JsonProperty("channel-version")]
    string ChannelVersion,
    [JsonProperty("latest-release")]
    string LatestRelease,
    [JsonProperty("latest-release-date")]
    string LatestReleaseDate,
    bool Security,
    [JsonProperty("latest-runtime")]
    string LatestRuntime,
    [JsonProperty("latest-sdk")]
    string LatestSdk,
    string Product,
    [JsonProperty("release-type")]
    string ReleaseType,
    [JsonProperty("support-phase")]
    string SupportPhase,
    [JsonProperty("eol-date")]
    string EOLDate,
    [JsonProperty("releases-json")]
    string ReleasesJSON
);