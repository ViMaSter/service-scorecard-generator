using System.Net;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models.GitHub;
using Serilog;

namespace ScorecardGenerator.Checks.PendingRenovateAzurePRs;

public class InfoGenerator
{
    private const int DEDUCTION_PER_ACTIVE_PULL_REQUEST = 20;

    public interface IInfo
    {
        public static bool FromURL(string url, out IInfo? info)
        {
            info = null;
            return false;
        }

        IList<BaseCheck.Deduction> GetDeductions(ILogger logger, Func<string, HttpResponseMessage> getHTTPRequest, string absolutePathToProjectFile);
    }

    private class AzureInfo : IInfo
    {
        private string _organization;
        private string _project;
        private string _repo;

        private AzureInfo(string organization, string project, string repo)
        {
            _organization = organization;
            _project = project;
            _repo = repo;
        }

        public static IInfo? FromURL(string url)
        {
            if (url.Contains("visualstudio"))
            {
                var pathSplit = url.Split('/');
                var gitIndex = Array.IndexOf(pathSplit, "_git");
                return new AzureInfo
                (
                    pathSplit[gitIndex - 2], 
                    pathSplit[gitIndex - 1], 
                    pathSplit[gitIndex + 1]
                );
            }
            
            if (url.Contains("dev.azure"))
            {
                var pathSplit = url.Split('/');
                var gitIndex = Array.IndexOf(pathSplit, "_git");
                return new AzureInfo
                (
                    pathSplit[gitIndex - 2], 
                    pathSplit[gitIndex - 1], 
                    pathSplit[gitIndex + 1]
                );
            }
            
            return null;
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
            var pullRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models.Azure.PullRequest>(pullRequestJSON)!.value;
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
                deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, DEDUCTION_PER_ACTIVE_PULL_REQUEST, $"There is an active pull request for {pr.repository.name} that is renovating {projectFileNameWithExtension}"));
            }

            return deductionsPerPR;
        }
    };

    private class GitHubInfo : IInfo
    {
        private string _organization;
        private string _repo;

        private GitHubInfo(string organization, string repo)
        {
            _organization = organization;
            _repo = repo;
        }

        public static IInfo? FromURL(string url)
        {
            if (!url.Contains("github"))
            {
                return null;
            }
            var pathSplit = url.Split('/');
            var gitIndex = Array.IndexOf(pathSplit, "github.com");
            return new GitHubInfo
            (
                pathSplit[gitIndex + 1],
                pathSplit[gitIndex + 2].Split('.').First()
            );
        }

        public override string ToString()
        {
            return $"{GetType().Name}: {_organization}/{_repo}";
        }

        // ReSharper disable InconsistentNaming
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnassignedGetOnlyAutoProperty
        private class GitHubPullRequest
        {
            public int number { get; }
            public GitHubPullRequestHead head { get; } = new();

            internal class GitHubPullRequestHead
            {
                public string @ref => "";
            }
        }
        // ReSharper restore UnassignedGetOnlyAutoProperty

        private class FilesChanged
        {
            public string filename => "";
        }
        // ReSharper restore ClassNeverInstantiated.Local
        // ReSharper restore InconsistentNaming

        public IList<BaseCheck.Deduction> GetDeductions(ILogger logger, Func<string, HttpResponseMessage> getHTTPRequest, string absolutePathToProjectFile)
        {
            var deductionsPerPR = new List<BaseCheck.Deduction>();
            var projectPullRequestsURL = $"https://api.github.com/repos/{_organization}/{_repo}/pulls?state=open";
            var allProjectPullRequests = getHTTPRequest(projectPullRequestsURL);
            var pullRequestJSON = allProjectPullRequests.Content.ReadAsStringAsync().Result;
            if (allProjectPullRequests.StatusCode != HttpStatusCode.OK)
            {
                logger.Error("Failed to get pull requests from {ProjectPullRequestsUrl}", projectPullRequestsURL);
                deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, 100, "Failed to get pull requests from {ProjectPullRequestsUrl}", projectPullRequestsURL));
                return deductionsPerPR;
            }
            var pullRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<List<GitHubPullRequest>>(pullRequestJSON)!;

            var renovatePullRequests = pullRequests.Where(pr => pr.head.@ref.Contains("renovate")).ToList();

            foreach (var pr in renovatePullRequests)
            {
                var allFilesChanged = new List<string>();

                var path = $"https://api.github.com/repos/{_organization}/{_repo}/pulls/{pr.number}/files";

                var filesChanged = getHTTPRequest(path);

                var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
                var filesChangedList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<FilesChanged>>(filesChangedJSON)!;
                allFilesChanged.AddRange(filesChangedList.Select(fc => fc.filename));

                var projectFileNameWithExtension = Path.GetFileName(absolutePathToProjectFile)!;
                if (!allFilesChanged.Any(fc => fc.EndsWith(projectFileNameWithExtension)))
                {
                    continue;
                }
                deductionsPerPR.Add(BaseCheck.Deduction.Create(logger, DEDUCTION_PER_ACTIVE_PULL_REQUEST, "Active pull request #{PRNumber} is renovating {ProjectFileNameWithExtension}", pr.number, projectFileNameWithExtension));
            }

            return deductionsPerPR;
        }
    }

    public static IInfo FromURL(string url)
    {
        // find all private classes deriving from IInfo and call FromURL on them; return the first non-null result
        var types = typeof(IInfo).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Contains(typeof(IInfo)));
        foreach (var type in types)
        {
            var method = type.GetMethod("FromURL");
            if (method == null)
            {
                continue;
            }
            var result = method.Invoke(null, new object[] {url});
            if (result != null)
            {
                return (IInfo)result;
            }
        }
        throw new Exception("No IInfo implementation found for URL: " + url);
    }
}