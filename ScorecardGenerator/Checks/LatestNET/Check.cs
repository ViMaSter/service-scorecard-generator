using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Serilog;

namespace ScorecardGenerator.Checks.LatestNET;

public partial class Check : BaseCheck
{
    private record ReleaseData(
        [JsonProperty("releases-index")]
        Release[] ReleasesIndex
    );

    private record Release(
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
    
    internal class ReleaseComparer : IComparer<Release>
    {
        int IComparer<Release>.Compare(Release? x, Release? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            var xSplit = x.ChannelVersion.Split(".");
            var xMajor = xSplit.Length > 0 ? int.Parse(xSplit[0]) : 0;
            var xMinor = xSplit.Length > 1 ? int.Parse(xSplit[1]) : 0;
            var xPatch = xSplit.Length > 2 ? int.Parse(xSplit[2]) : 0;
            var xFull = xMajor * 10000 + xMinor * 100 + xPatch;
        
            var ySplit = y.ChannelVersion.Split(".");
            var yMajor = ySplit.Length > 0 ? int.Parse(xSplit[0]) : 0;
            var yMinor = ySplit.Length > 1 ? int.Parse(xSplit[1]) : 0;
            var yPatch = ySplit.Length > 2 ? int.Parse(xSplit[2]) : 0;
            var yFull = yMajor * 10000 + yMinor * 100 + yPatch;

            return yFull.CompareTo(xFull);
        }
    }
    
    private readonly int _newestMajor;
    private readonly string _newestText;

    public Check(ILogger logger) : base(logger)
    {
        var httpClient = new HttpClient();
        var jsonResponse = httpClient.GetStringAsync("https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json").Result;
        var parsed = JsonConvert.DeserializeObject<ReleaseData>(jsonResponse);
        var latestNonPreviewVersion = parsed!.ReleasesIndex.Where(release => release.SupportPhase != "preview").OrderDescending(new ReleaseComparer()).First();
        _newestMajor = int.Parse(latestNonPreviewVersion.ChannelVersion.Split(".").First());
        _newestText = latestNonPreviewVersion.ChannelVersion;
    }

    protected override IList<Deduction> Run(string workingDirectory, string relativePathToServiceRoot)
    {
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var csprojFiles = Directory.GetFiles(absolutePathToServiceRoot, "*.csproj", SearchOption.TopDirectoryOnly);
        if (!csprojFiles.Any())
        {
            return new List<Deduction> {Deduction.Create(Logger, 100, "No csproj file found at {Location}", absolutePathToServiceRoot)};
        }
        var csproj = XDocument.Load(csprojFiles.First());
        var targetFramework = csproj.XPathSelectElement("/Project/PropertyGroup/TargetFramework")?.Value;
        if (string.IsNullOrEmpty(targetFramework))
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No <TargetFramework> element found in {CsProj}", csprojFiles.First()) };
        }

        if (!targetFramework.Contains('.')) // only way to discern .NET Framework is to check for a dot (https://learn.microsoft.com/en-us/dotnet/standard/frameworks#latest-versions)
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Service uses {Current}, latest available is {Latest}; using .NET Framework deducts all points", targetFramework, _newestText) };
        }
        
        var currentMajor = int.Parse(FirstNumber().Match(targetFramework).Value);
        if (currentMajor > _newestMajor)
        {
            Logger.Warning("Current major version ({Current}) is higher than the newest major version ({Latest})", currentMajor, _newestText);
            return new List<Deduction> { Deduction.Create(Logger, 5, "Service uses ({Current}) latest available is only ({Latest})", targetFramework, _newestText) };
        }
        if (currentMajor == _newestMajor)
        {
            return new List<Deduction>();
        }
        
        var offset = 100-(int)Math.Round(((double)currentMajor / _newestMajor) * 100);
        return new List<Deduction> { Deduction.Create(Logger, offset, "Service uses {CurrentText}, latest available is {LatestText} ({CurrentMajor}/{LatestMajor}={Offset}%)", targetFramework, _newestText, currentMajor, _newestMajor, 100-offset) };
    }

    [GeneratedRegex("\\d")]
    private static partial Regex FirstNumber();
}