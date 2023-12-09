using ScorecardGenerator.Checks.RemainingDependencyUpgrades.Models.GitHub;
using Serilog;

namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo;

// ReSharper disable once UnusedType.Global - Used via InfoGenerator.FromURL
public class AzureInfo : IInfo
{
    private readonly string _organization;
    private readonly string _project;
    private readonly string _repo;

    private AzureInfo(string organization, string project, string repo)
    {
        _organization = organization;
        _project = project;
        _repo = repo;
    }
        
    private enum Domain
    {
        Azure,
        VisualStudio,
        Neither
    }
        
    private enum Protocol
    {
        HTTPS,
        SSH,
        Neither
    }

    public static IInfo? FromURL(string url)
    {
        var pathSplit = url.Split('/');
        var protocol = url.Contains("ssh") ? Protocol.SSH : url.Contains("https") ? Protocol.HTTPS : Protocol.Neither;
        var domain = url.Contains("dev.azure") ? Domain.Azure : url.Contains("visualstudio") ? Domain.VisualStudio : Domain.Neither;
        if (protocol == Protocol.Neither || domain == Domain.Neither)
        {
            return null;
        }
        return (protocol, domain) switch
        {
            (Protocol.SSH, Domain.VisualStudio) => new AzureInfo
            (
                pathSplit[1], 
                pathSplit[2], 
                pathSplit[3].Split(" ")[0]
            ),
            (Protocol.SSH, Domain.Azure) => new AzureInfo
            (
                pathSplit[1], 
                pathSplit[2], 
                pathSplit[3].Split(" ")[0]
            ),
            (Protocol.HTTPS, Domain.VisualStudio) => new AzureInfo
            (
                pathSplit[2].Split(".")[0], 
                pathSplit[3], 
                pathSplit[5].Split(" ")[0]
            ),
            (Protocol.HTTPS, Domain.Azure) => new AzureInfo
            (
                pathSplit[3], 
                pathSplit[4], 
                pathSplit[6].Split(" ")[0]
            ),
            _ => throw new Exception("Unknown URL format: " + url) 
        };
    }

    public override string ToString()
    {
        return $"{GetType().Name}: {_organization}/{_project}/{_repo}";
    }

    public IList<BaseCheck.Deduction> GetDeductions(ILogger logger, Func<string, HttpResponseMessage> getHTTPRequest, string absolutePathToProjectFile)
    {
        var deductionsPerPR = new List<BaseCheck.Deduction>();
        var projectPullRequestsURL = $"https://dev.azure.com/{_organization}/{_project}/_apis/git/pullrequests?api-version=7.0&searchCriteria.status=active";
        var allProjectPullRequests = getHTTPRequest(projectPullRequestsURL);
        var pullRequestJSON = allProjectPullRequests.Content.ReadAsStringAsync().Result;
        var pullRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<ScorecardGenerator.Checks.RemainingDependencyUpgrades.Models.Azure.PullRequest>(pullRequestJSON)!.value;
        var renovatePullRequests = pullRequests.Where(pr => pr.repository.name == _repo && pr.sourceRefName.Contains("renovate")).ToList();

        foreach (var pr in renovatePullRequests)
        {
            var allFilesChanged = new List<string>();

            var path = $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations?api-version=7.0";

            var iterations = getHTTPRequest(path);

            var iterationsJSON = iterations.Content.ReadAsStringAsync().Result;
            var iterationsList = Newtonsoft.Json.JsonConvert.DeserializeObject<Iteration>(iterationsJSON)!.value;
            foreach (var iteration in iterationsList)
            {
                var url = $"https://dev.azure.com/{_organization}/{_project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations/{iteration.id}/changes?api-version=7.0";
                var filesChanged = getHTTPRequest(url);
                var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
                var filesChangedList = Newtonsoft.Json.JsonConvert.DeserializeObject<Changes>(filesChangedJSON)!.changeEntries;
                allFilesChanged.AddRange(filesChangedList.Select(fc => fc.item.path));
            }

            var projectFileNameWithExtension = Path.GetFileName(absolutePathToProjectFile)!;
            if (!allFilesChanged.Any(fc => fc.EndsWith(projectFileNameWithExtension)))
            {
                continue;
            }
            deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, IInfo.DEDUCTION_PER_ACTIVE_PULL_REQUEST, "PR #{PRPullRequestId} - {PRTitle}", pr.pullRequestId, pr.title));
        }

        return deductionsPerPR;
    }
};