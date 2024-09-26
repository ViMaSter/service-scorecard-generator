using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Polly;
using Polly.Retry;
using ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo;
using Serilog;

namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades;

public class Check : BaseCheck
{
    private readonly AzurePAT _azurePAT;
    private readonly GitHubPAT _githubPAT;

    // ReSharper disable once ClassNeverInstantiated.Global instantiated via DI
    public class AzurePAT
    {
        public string Value { get; }

        public AzurePAT(string value)
        {
            Value = value;
        }
    }
    public class GitHubPAT
    {
        public string Value { get; }

        public GitHubPAT(string value)
        {
            Value = value;
        }
    }

    public Check(ILogger logger, AzurePAT azurePAT, GitHubPAT githubPAT, HttpMessageHandler? overrideMessageHandler = null) : base(logger)
    {
        _azurePAT = azurePAT;
        _githubPAT = githubPAT;
        _client = new HttpClient(overrideMessageHandler ?? new HttpClientHandler())
        {
            DefaultRequestHeaders =
            {
                UserAgent = { ProductInfoHeaderValue.Parse("ScorecardGenerator/" + typeof(Check).Assembly.GetName().Version!.ToString(3)) },
                Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") }
            }
        };
        _client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }
    
    private static readonly Dictionary<string, HttpResponseMessage> Data = new();
    private readonly HttpClient _client;

    private readonly RetryPolicy<HttpResponseMessage> _retryPolicy = Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode && r.StatusCode is < HttpStatusCode.BadRequest or >= HttpStatusCode.InternalServerError)
        .WaitAndRetry(new List<TimeSpan>
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(10)
        });

    private HttpResponseMessage GetHTTPRequest(string url)
    {
        if (Data.TryGetValue(url, out var value))
        {
            return value;
        }

        if (url.ToLower().Contains("github"))
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _githubPAT.Value);
        }
        else
        {
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{_azurePAT.Value}")));
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
                Arguments = "remote -v",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WorkingDirectory = serviceRootDirectory
            }
        };
        process.Start();
        process.WaitForExit();
        var allLines = process.StandardOutput.ReadToEnd().ReplaceLineEndings();
        var azureInfoFromGit = allLines.Split(Environment.NewLine).Where(a=>!string.IsNullOrWhiteSpace(a)).Select(a=>a.Split("\t")[1]).Select(InfoGenerator.FromURL).ToList();

        if (!azureInfoFromGit.Any())
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "No Azure DevOps remotes found for {ServiceRootDirectory}; can't check for open pull requests", serviceRootDirectory) };
        }

        if (azureInfoFromGit.Count > 1)
        {
            Logger.Warning("Multiple Azure DevOps remotes found for {ServiceRootDirectory}; using {Info}", serviceRootDirectory, azureInfoFromGit.First());
        }

        var azureInfo = azureInfoFromGit.First();

        var deductionsPerPR = azureInfo.GetDeductions(Logger, GetHTTPRequest, absolutePathToProjectFile);

        return deductionsPerPR;
    }
}