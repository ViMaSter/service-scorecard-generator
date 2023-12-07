using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ScorecardGenerator.Checks.PendingRenovateAzurePRs.Models;
using Serilog;
using Polly;
using Polly.Retry;

namespace ScorecardGenerator.Checks.PendingRenovateAzurePRs;

public class Check : BaseCheck
{
    // ReSharper disable once ClassNeverInstantiated.Global instantiated via DI
    public class AzurePAT
    {
        public string Value { get; }

        public AzurePAT(string value)
        {
            Value = value;
        }
    }

    public Check(ILogger logger, AzurePAT azurePAT, HttpMessageHandler? overrideMessageHandler = null) : base(logger)
    {
        _client = new HttpClient(overrideMessageHandler ?? new HttpClientHandler())
        {
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{azurePAT.Value}")))
            }
        };
    }

    private const int DEDUCTION_PER_ACTIVE_PULL_REQUEST = 20;

    private static readonly Dictionary<string, HttpResponseMessage> Data = new();
    private readonly HttpClient _client;

    private readonly RetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .WaitAndRetry(new List<TimeSpan>()
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(10),
        });

    private HttpResponseMessage GetHTTPRequest(string url)
    {
        if (Data.TryGetValue(url, out var value))
        {
            return value;
        }

        Data[url] = _retryPolicy.Execute(() => _client.GetAsync(url).Result);

        File.WriteAllText(Path.Join(Directory.GetCurrentDirectory(), url.Replace("/", "_")), $"{Data[url].StatusCode}{Environment.NewLine}{Data[url].Content.ReadAsStringAsync().Result}");
        return Data[url];
    }

    

    protected override IList<Deduction> Run(string absolutePathToProjectFile)
    {
        var serviceRootDirectory = Path.GetDirectoryName(absolutePathToProjectFile)!;
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
        var azureInfoFromGit = allLines.Split(Environment.NewLine).Select(InfoGenerator.FromURL).ToList();

        if (!azureInfoFromGit.Any())
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No Azure DevOps remotes found for {ServiceRootDirectory}; can't check for open pull requests", serviceRootDirectory) };
        }

        if (azureInfoFromGit.Count > 1)
        {
            Logger.Warning("Multiple Azure DevOps remotes found for {ServiceRootDirectory}; using {Organization}/{Project}/{Repo}", serviceRootDirectory, azureInfoFromGit.First().organization, azureInfoFromGit.First().project, azureInfoFromGit.First().repo);
        }

        var azureInfo = azureInfoFromGit.First();

        var projectPullRequestsURL = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/pullrequests?api-version=7.0&searchCriteria.status=active";
        var allProjectPullRequests = GetHTTPRequest(projectPullRequestsURL);
        var pullRequestJSON = allProjectPullRequests.Content.ReadAsStringAsync().Result;
        var pullRequests = Newtonsoft.Json.JsonConvert.DeserializeObject<PullRequest>(pullRequestJSON)!.value;
        var renovatePullRequests = pullRequests.Where(pr => pr.repository.name == azureInfo.repo && pr.sourceRefName.Contains("renovate")).ToList();

        var deductionsPerPR = new List<Deduction>();
        foreach (var pr in renovatePullRequests)
        {
            var allFilesChanged = new List<string>();

            var path = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations?api-version=7.0";

            var iterations = GetHTTPRequest(path);

            var iterationsJSON = iterations.Content.ReadAsStringAsync().Result;
            var iterationsList = Newtonsoft.Json.JsonConvert.DeserializeObject<Iteration>(iterationsJSON)!.value;
            foreach (var iteration in iterationsList)
            {
                var url = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations/{iteration.id}/changes?api-version=7.0";
                var filesChanged = GetHTTPRequest(url);
                var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
                var filesChangedList = Newtonsoft.Json.JsonConvert.DeserializeObject<Changes>(filesChangedJSON)!.changeEntries;
                allFilesChanged.AddRange(filesChangedList.Select(fc => fc.item.path));
            }

            var projectFileNameWithExtension = Path.GetFileName(absolutePathToProjectFile)!;
            if (!allFilesChanged.Any(fc => fc.EndsWith(projectFileNameWithExtension)))
            {
                continue;
            }
            deductionsPerPR.Add(Deduction.Create(Logger, DEDUCTION_PER_ACTIVE_PULL_REQUEST, "PR {PRNumber} - {Title}", pr.pullRequestId, pr.title));
        }

        return deductionsPerPR;
    }
}