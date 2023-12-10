using System.Net;
using System.Web;
using Newtonsoft.Json;
using ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo.GitHub.Models;
using Serilog;

namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo;

// ReSharper disable once UnusedType.Global - Used via InfoGenerator.FromURL
public class GitHubInfo : IInfo
{
    private readonly string _organization;
    private readonly string _repo;

    private GitHubInfo(string organization, string repo)
    {
        _organization = organization;
        _repo = repo;
    }

    // ReSharper disable once UnusedMember.Global - Used via InfoGenerator.FromURL
    public static IInfo? FromURL(string url)
    {
        if (!url.Contains("github"))
        {
            return null;
        }
        var pathSplit = url.Split('/');
        if (url.Contains('@'))
        {
            return new GitHubInfo
            (
                pathSplit[0].Split(':')[1],
                pathSplit[1].Split('.')[0]
            );
        }
        
        return new GitHubInfo
        (
            pathSplit[3],
            pathSplit[4].Split(".")[0]
        );
    }

    public override string ToString()
    {
        return $"{GetType().Name}: {_organization}/{_repo}";
    }
    
    public IList<BaseCheck.Deduction> GetDeductions(ILogger logger, Func<string, HttpResponseMessage> getHTTPRequest, string absolutePathToProjectFile)
    {
        var deductionsPerPR = new List<BaseCheck.Deduction>();
        var projectPullRequestsURL = $"https://api.github.com/repos/{_organization}/{_repo}/pulls?state=open";
        var request = getHTTPRequest(projectPullRequestsURL);
        var requestBody = request.Content.ReadAsStringAsync().Result;
        if (request.StatusCode != HttpStatusCode.OK)
        {
            deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, 100, "Failed to get pull requests from {ProjectPullRequestsUrl}{Newline}{Status}{Newline2}{Body}", projectPullRequestsURL, "&#013;", (int)request.StatusCode, "&#013;", HttpUtility.HtmlEncode(requestBody)));
            return deductionsPerPR;
        }
        var pullRequests = JsonConvert.DeserializeObject<List<GitHubPullRequest>>(requestBody)!;

        var dependabotPullRequests = pullRequests.Where(pr => pr.head.@ref.Contains("dependabot")).ToList();

        foreach (var pr in dependabotPullRequests)
        {
            var allFilesChanged = new List<string>();

            var path = $"https://api.github.com/repos/{_organization}/{_repo}/pulls/{pr.number}/files";

            var filesChanged = getHTTPRequest(path);

            var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
            var filesChangedList = JsonConvert.DeserializeObject<List<FilesChanged>>(filesChangedJSON)!;
            allFilesChanged.AddRange(filesChangedList.Select(fc => fc.filename));

            var projectFileNameWithExtension = Path.GetFileName(absolutePathToProjectFile);
            if (!allFilesChanged.Any(fc => fc.EndsWith(projectFileNameWithExtension)))
            {
                continue;
            }
            deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, IInfo.DEDUCTION_PER_ACTIVE_PULL_REQUEST, "Active pull request #{PRNumber} is renovating {ProjectFileNameWithExtension}", pr.number, projectFileNameWithExtension));
        }

        return deductionsPerPR;
    }
}