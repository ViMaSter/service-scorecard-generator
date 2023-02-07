using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Visualizer;
using Serilog;

namespace ScorecardGenerator;

internal class GenerateScorecard
{
    private record Checks
    (
        List<IRunCheck> Gold, List<IRunCheck> Silver, List<IRunCheck> Bronze
    )
    {
        public const int GoldWeight = 10;
        public const int SilverWeight = 5;
        public const int BronzeWeight = 1;
    }
        
    private readonly IEnumerable<string> _directoriesInWorkingDirectory;
    private readonly ILogger _logger;

    public GenerateScorecard(ILogger logger)
    {
        _logger = logger;
        // find all directories in working directory that contain a csproj file
        _directoriesInWorkingDirectory = Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories)
            .Where(directory => Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any());
    }
    
    public void Execute(string azurePAT)
    {
        var checks = new Checks
        (
            new List<IRunCheck>()
            {
                new HasNET7(_logger),
                new HintPathCounter(_logger),
                new PendingRenovatePRs(_logger, azurePAT)
            },
            new List<IRunCheck>(),
            new List<IRunCheck>()
        );
        var listByGroup = new Dictionary<string, IList<string>>
        {
            { nameof(checks.Gold), checks.Gold.Select(check => check.GetType().Name).ToList() },
            { nameof(checks.Silver), checks.Silver.Select(check => check.GetType().Name).ToList() },
            { nameof(checks.Bronze), checks.Bronze.Select(check => check.GetType().Name).ToList() },
        };
        
        var scoreForServiceByCheck = _directoriesInWorkingDirectory.ToDictionary(Utilities.RootDirectoryToProjectNameFromCsproj, serviceRootDirectory =>
        {
            var goldScoreByCheck = checks.Gold.ToDictionary(check => check.GetType().Name, check => check.Run(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var silverScoreByCheck = checks.Silver.ToDictionary(check => check.GetType().Name, check => check.Run(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var bronzeScoreByCheck = checks.Bronze.ToDictionary(check => check.GetType().Name, check => check.Run(Directory.GetCurrentDirectory(), serviceRootDirectory.Replace(Directory.GetCurrentDirectory(), "")));
            var totalScore = new[]
            {
                (decimal)goldScoreByCheck.Values.Sum()   * Checks.GoldWeight,
                (decimal)silverScoreByCheck.Values.Sum() * Checks.SilverWeight,
                (decimal)bronzeScoreByCheck.Values.Sum() * Checks.BronzeWeight
            }.Sum();

            var totalChecks = goldScoreByCheck.Count * Checks.GoldWeight + silverScoreByCheck.Count * Checks.SilverWeight + bronzeScoreByCheck.Count * Checks.BronzeWeight;
            var average = (int)Math.Round(totalScore / totalChecks);
            var scoreByCheck = goldScoreByCheck
                                                    .Concat(silverScoreByCheck)
                                                    .Concat(bronzeScoreByCheck)
                                                    .ToDictionary(a => a.Key, a => a.Value);
            return new RunInfo.ServiceScorecard(scoreByCheck, average);
        });

        var runInfo = new RunInfo(listByGroup, scoreForServiceByCheck);

        File.WriteAllText("result.md", new ColorizedHTMLTableVisualizer(_logger).ToMarkdown(runInfo));
    }
}

internal partial class PendingRenovatePRs : IRunCheck
{
    private readonly ILogger _logger;
    private readonly string _azurePAT;

    public PendingRenovatePRs(ILogger logger, string azurePAT)
    {
        _logger = logger;
        _azurePAT = azurePAT;
    }

    public int Run(string workingDirectory, string relativePathToServiceRoot)
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
            line = line.Replace("\t", " ").Replace("  ",  " ").Split(" ")[1];
            string organization, project, repo;
            if (line.Contains("@"))
            {
                var parts = line.Split("/");
                organization = parts[^3];
                project = parts[^2];
                repo = parts[^1];
            }
            else
            {
                organization = line.Split('.')[0];
                // split path by /
                var pathSplit = line.Split('/');
                // get index of "_git" entry in pathSplit array
                var gitIndex = Array.IndexOf(pathSplit, "_git");
                // get previous group as project
                project = pathSplit[gitIndex - 1];
                // get next group as repo
                repo = pathSplit[gitIndex + 1];         
            }

            return new { organization, project, repo };
        }).ToList();

        if (!azureInfoFromGit.Any())
        {
            _logger.Error("No Azure DevOps remotes found for {ServiceRootDirectory}; can't check for open pull requests", serviceRootDirectory);
            return 0;
        }

        if (azureInfoFromGit.Count > 1)
        {
            _logger.Warning("Multiple Azure DevOps remotes found for {ServiceRootDirectory}; using {Organization}/{Project}/{Repo}", serviceRootDirectory, azureInfoFromGit.First().organization, azureInfoFromGit.First().project, azureInfoFromGit.First().repo);
        }

        var azureInfo = azureInfoFromGit.First();
        
        // use azure http api and pat to find all open PRs that contain renovate in branch name
        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_azurePAT}")));
                                    // GET 

        var allProjectPullRequests = client.GetAsync($"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/pullrequests?api-version=7.0&searchCriteria.status=active").Result;
        if (!allProjectPullRequests.IsSuccessStatusCode)
        {
            _logger.Error("Couldn't fetch open PRs for {ServiceRootDirectory}; check verbose output for response", serviceRootDirectory);
            _logger.Verbose("response: {Response}", allProjectPullRequests);
            return 0;
        }
        
        var pullRequestJSON = allProjectPullRequests.Content.ReadAsStringAsync().Result;
        var outp = Newtonsoft.Json.JsonConvert.DeserializeObject<PullRequest>(pullRequestJSON)!.value;
        var pullRequests = outp.Where(pr => pr.repository.name == azureInfo.repo && pr.sourceRefName.Contains("renovate")).ToList();

        // get all files changed in each PR
        var relevantPRIDs = new List<int>();
        foreach (var pr in pullRequests)
        {
            var allFilesChanged = new List<string>();

            var path = $"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations?api-version=7.0";
            
            var iterations = client.GetAsync(path).Result;
            if (!iterations.IsSuccessStatusCode)
            {
                _logger.Error("Couldn't fetch iterations for PR {PRNumber} in {ServiceRootDirectory}; check verbose output for response", pr.pullRequestId, serviceRootDirectory);
                _logger.Verbose("response: {Response}", iterations);
                return 0;
            }

            var iterationsJSON = iterations.Content.ReadAsStringAsync().Result;
            var iterationsList = Newtonsoft.Json.JsonConvert.DeserializeObject<Iteration>(iterationsJSON)!.value;
            foreach (var iteration in iterationsList)
            {
                var filesChanged = client.GetAsync($"https://dev.azure.com/{azureInfo.organization}/{azureInfo.project}/_apis/git/repositories/{pr.repository.id}/pullRequests/{pr.pullRequestId}/iterations/{iteration.id}/changes?api-version=7.0").Result;
                if (!filesChanged.IsSuccessStatusCode)
                {
                    _logger.Error("Couldn't fetch file changes for PR {PRNumber} in {ServiceRootDirectory}; check verbose output for response", pr.pullRequestId, serviceRootDirectory);
                    _logger.Verbose("response: {Response}", filesChanged);
                    return 0;
                }

                var filesChangedJSON = filesChanged.Content.ReadAsStringAsync().Result;
                var filesChangedList = Newtonsoft.Json.JsonConvert.DeserializeObject<Changes>(filesChangedJSON)!.changeEntries;
                allFilesChanged.AddRange(filesChangedList.Select(fc => fc.item.path));
            }
            
            // if any of the files changed in the PR are in the service root directory, add the PR to the list of PRs to check
            if (allFilesChanged.Any(fc => fc.Contains(relativePathToServiceRoot)))
            {
                _logger.Information("Found open PR {PRNumber} in {ServiceRootDirectory}", pr.pullRequestId, serviceRootDirectory);
                relevantPRIDs.Add(pr.pullRequestId);
            }
        }

        var openPRsCount = relevantPRIDs.Count;
        return Math.Max(0, 100 - openPRsCount * 20);
    }
}