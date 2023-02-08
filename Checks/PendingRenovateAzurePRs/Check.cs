using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models;
using Serilog;

namespace ScorecardGenerator.Checks.PendingRenovateAzurePRs;

internal class Check : BaseCheck
{
    private readonly string _azurePAT;

    public Check(ILogger logger, string azurePAT) : base(logger)
    {
        _azurePAT = azurePAT;
    }

    const int PenaltyPerActivePullRequest = 20;

    protected override int Run(string workingDirectory, string relativePathToServiceRoot)
    {
        var serviceRootDirectory = Path.Join(workingDirectory, relativePathToServiceRoot);
        // use git command to find all remotes
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"remote -v",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = serviceRootDirectory
            }
        };
        process.Start();
        process.WaitForExit();
        var allLines = process.StandardOutput.ReadToEnd();
        var azureInfoFromGit = allLines.Split(Environment.NewLine).Where(line => line.Contains("visualstudio") || line.Contains("dev.azure")).Select(line =>
        {
            line = line.Replace("\t", " ").Replace("  ", " ").Split(" ")[1];
            string organization, project, repo;
            if (line.Contains("http"))
            {
                if (line.Contains("dev.azure"))
                {
                    var pathSplit = line.Split('/');
                    var gitIndex = Array.IndexOf(pathSplit, "_git");
                    organization = pathSplit[gitIndex - 2];
                    project = pathSplit[gitIndex - 1];
                    repo = pathSplit[gitIndex + 1];
                }
                else
                {
                    organization = line.Split('.')[0].Split("/")[1];
                    var pathSplit = line.Split('/');
                    var gitIndex = Array.IndexOf(pathSplit, "_git");
                    project = pathSplit[gitIndex - 1];
                    repo = pathSplit[gitIndex + 1];
                }
            }
            else
            {
                var parts = line.Split("/");
                organization = parts[^3];
                project = parts[^2];
                repo = parts[^1];
            }

            return new { organization, project, repo };
        }).ToList();

        if (!azureInfoFromGit.Any())
        {
            Logger.Error("No Azure DevOps remotes found for {ServiceRootDirectory}; can't check for open pull requests", serviceRootDirectory);
            return 0;
        }

        if (azureInfoFromGit.Count > 1)
        {
            Logger.Warning("Multiple Azure DevOps remotes found for {ServiceRootDirectory}; using {Organization}/{Project}/{Repo}", serviceRootDirectory, azureInfoFromGit.First().organization, azureInfoFromGit.First().project, azureInfoFromGit.First().repo);
        }

        var azureInfo = azureInfoFromGit.First();

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_azurePAT}")));
        var projectPullRequestsURL = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/pullrequests?api-version=7.0&searchCriteria.status=active";
        var allProjectPullRequests = client.GetAsync(projectPullRequestsURL).Result;
        if (!allProjectPullRequests.IsSuccessStatusCode)
        {
            Logger.Error("Couldn't fetch open PRs for {ServiceRootDirectory}; check verbose output for response", serviceRootDirectory);
            Logger.Verbose("response: {Response}", allProjectPullRequests);
            Logger.Verbose("url: {Url}", projectPullRequestsURL);
            return 0;
        }

        var pullRequestJSON = allProjectPullRequests.Content.ReadAsStringAsync().Result;
        var pullRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<PullRequest>(pullRequestJSON)!.value;
        var renovatePullRequests = pullRequests.Where(pr => pr.repository.name == azureInfo.repo && pr.sourceRefName.Contains("renovate")).ToList();

        // get all files changed in each PR
        var relevantPullRequestIDs = new List<int>();
        foreach (var pr in renovatePullRequests)
        {
            var allFilesChanged = new List<string>();

            var path = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations?api-version=7.0";

            var iterations = client.GetAsync(path).Result;
            if (!iterations.IsSuccessStatusCode)
            {
                Logger.Error("Couldn't fetch iterations for PR {PRNumber} in {ServiceRootDirectory}; check verbose output for response", pr.pullRequestId, serviceRootDirectory);
                Logger.Verbose("response: {Response}", iterations);
                Logger.Verbose("path: {Path}", path);
                return 0;
            }

            var iterationsJSON = iterations.Content.ReadAsStringAsync().Result;
            var iterationsList = Newtonsoft.Json.JsonConvert.DeserializeObject<Iteration>(iterationsJSON)!.value;
            foreach (var iteration in iterationsList)
            {
                var url = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations/{iteration.id}/changes?api-version=7.0";
                var filesChanged = client.GetAsync(url).Result;
                if (!filesChanged.IsSuccessStatusCode)
                {
                    Logger.Error("Couldn't fetch file changes for PR {PRNumber} in {ServiceRootDirectory}; check verbose output for response", pr.pullRequestId, serviceRootDirectory);
                    Logger.Verbose("response: {Response}", filesChanged);
                    Logger.Verbose("url: {Url}", url);
                    return 0;
                }

                var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
                var filesChangedList = Newtonsoft.Json.JsonConvert.DeserializeObject<Changes>(filesChangedJSON)!.changeEntries;
                allFilesChanged.AddRange(filesChangedList.Select(fc => fc.item.path));
            }

            if (!allFilesChanged.Any(fc => fc.Contains(relativePathToServiceRoot)))
            {
                continue;
            }
            Logger.Information("Found open PR #{PRNumber} for {ServiceRootDirectory}; deducting {Penalty} points", pr.pullRequestId, serviceRootDirectory, PenaltyPerActivePullRequest);
            relevantPullRequestIDs.Add(pr.pullRequestId);
        }

        var openPRsCount = relevantPullRequestIDs.Count;
        return Math.Max(0, 100 - openPRsCount * PenaltyPerActivePullRequest);
    }
}