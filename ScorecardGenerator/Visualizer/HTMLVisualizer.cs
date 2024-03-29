using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Markdig;
using Newtonsoft.Json;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using Serilog;

namespace ScorecardGenerator.Visualizer;

public partial class HTMLVisualizer : IVisualizer
{
    private readonly ILogger _logger;
    private readonly string _outputPath;
    private readonly string _dayOfGeneration;
    private const string QUESTION_MARK = "<sup>&nbsp;<b><i><u>?</u></i></b></sup>";
    private const string FILE_NAME = "index";
    private const string RESOURCE_NAME = "ScorecardGenerator.Visualizer.HTMLVisualizer.html";

    public HTMLVisualizer(ILogger logger, string outputPath)
    {
        _logger = logger.ForContext<HTMLVisualizer>();
        _outputPath = outputPath;
        _dayOfGeneration = $"{DateTime.Now:yyyy-MM-dd}";
        if (!Directory.Exists(_outputPath))
        {
            Directory.CreateDirectory(_outputPath);
        }
    }
    
    public void Visualize(RunInfo runInfo)
    {
        var infoFromSevenDaysAgo = Get7DaysAgo();

        var lastUpdatedAt = DateTime.Now;
        
        const string HEADER_ELEMENT = "th";
        const string COLUMN_ELEMENT = "td";
        string ToElement(string element, IEnumerable<TableContent> columns)
        {
            return $"<tr>{string.Join("", columns.Select(entry => $"<{element} title=\"{entry.Title}\" colspan=\"{entry.Colspan}\">{entry.Content}</{element}>"))}</tr>";
        }

        var runInfoJSON = JsonConvert.SerializeObject(runInfo);
        
        var headers = ToElement(HEADER_ELEMENT, runInfo.Checks.Values.SelectMany(checksInGroup=>checksInGroup).Select(check => new TableContent(check.Name, "")).Prepend("ServiceName").Append("Average"));
        var groupData = ToElement(HEADER_ELEMENT, runInfo.Checks.Where(group => group.Value.Any()).Select(group=>new TableContent(group.Key, "", group.Value.Count)).Prepend("   ").Append("   "));
        
        var output = runInfo.ServiceScores.Select(pair =>
        {
            var (fullPathToService, (scoreByCheckName, average)) = pair;
            var serviceName = $"<span>{Path.GetFileNameWithoutExtension(fullPathToService)}{QUESTION_MARK}</span>";
            return ToElement(COLUMN_ELEMENT, scoreByCheckName.Select(check => FormatJustifiedScore(check.Value, GetDeductions(_logger, infoFromSevenDaysAgo, fullPathToService, check))).Prepend(new TableContent(serviceName, fullPathToService)).Append(ColorizeAverageScore(average)));
        });
        
        _logger.Information("Generated scorecard at {LastUpdatedAt}", lastUpdatedAt);
        
        var assembly = Assembly.GetExecutingAssembly();
        string? html;
        using (var stream = assembly.GetManifestResourceStream(RESOURCE_NAME)!)
        {
            using var reader = new StreamReader(stream);
            html = reader.ReadToEnd();
        }

        var headline = $"Service Scorecard for {_dayOfGeneration}";
        
        var parameters = new Dictionary<string, string>
        {
            {"headline", headline},
            {"table", $"<table id=\"service-scorecard\">{string.Join(Environment.NewLine, output.Prepend(headers).Prepend(groupData).Prepend(""))}</table>" },
            {"data", runInfoJSON},
            {"lists", GenerateCheckHTML(runInfo)},
            {"lastUpdatedAt", lastUpdatedAt.ToString("O")}
        };
        
        WriteGeneratedOutput($"{FILE_NAME}.html", parameters.Aggregate(html, (current, parameter)=> current.Replace($"@{parameter.Key}", parameter.Value.ToString())));
    }
    
    private void WriteGeneratedOutput(string path, string content)
    {
        File.WriteAllText(Path.Join(_outputPath, path), $"{content}");
    }

    private string GenerateCheckHTML(RunInfo runInfo)
    {
        var htmlContent = "";
        foreach (var (checkName, infoPageContent) in runInfo.Checks.Values.SelectMany(checks => checks))
        {
            var content = infoPageContent.ReplaceLineEndings().Split(Environment.NewLine).Where(line => !line.StartsWith("# ")).Aggregate((current, next) => $"{current}{Environment.NewLine}{next}").Trim();
            htmlContent += $"<li><details><summary>{checkName}</summary><div>{Markdown.ToHtml(content)}</div></details></li>";
        }

        return htmlContent;
    }

    public static string RemoveDates(string content) => NoScriptFilter().Replace(content, "").Replace("YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd")).ReplaceLineEndings();

    private RunInfo? Get7DaysAgo()
    {
        var path = Path.Join(_outputPath, $"{FILE_NAME}.html");
        var sevenDaysAgo = DateTime.Now.Subtract(TimeSpan.FromDays(7));
        
        var gitLog = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "log -n 1000 --format=format:\"%h %ai\" --date=iso",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _outputPath
        });
        var commits = gitLog?.StandardOutput.ReadToEnd().Trim();
        _logger.Information("git log stdout: {StdOut}", commits);
        _logger.Information("git log stderr: {StdErr}", gitLog?.StandardError.ReadToEnd().Trim());
        if (string.IsNullOrEmpty(commits))
        {
            _logger.Information("No commits found skipping git diff");
            return null;
        }

        var sortedCommits = commits
            .Replace("\r", Environment.NewLine)
            .Replace("\n", Environment.NewLine)
            .Split(Environment.NewLine)
            .Where(line=>!string.IsNullOrEmpty(line))
            .Select(line =>
            {
                var list = line.Split(" ");
                return (DateTime.Parse(string.Join(" ", list.Skip(1))), list.First());
            })
            .OrderBy(dateAndHash=>
            {
                var (commitDate, _) = dateAndHash;
                return Math.Abs((commitDate - sevenDaysAgo).TotalSeconds);
            });
            
        var commitToUse = sortedCommits.First();
        
        _logger.Information("Diffing with commit: {Commit}", commitToUse);
            
        var arguments = $"show {commitToUse.Item2}:./{Path.GetFileName(path)} ";
        _logger.Information("running: git {Arguments}", arguments);
        var sevenDaysAgoContent = "";
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WorkingDirectory = _outputPath
                }
            };
            process.Start();
            process.WaitForExitAsync().ConfigureAwait(false).GetAwaiter();
            sevenDaysAgoContent = process.StandardOutput.ReadToEnd();
        }

        if (string.IsNullOrEmpty(sevenDaysAgoContent))
        {
            _logger.Information("No history available in that commit; skipping diff");
            return null;
        }
        
        _logger.Information("sevenDaysAgoContent: {SevenDaysAgoContent}", sevenDaysAgoContent);

        var lastLineOfFile = sevenDaysAgoContent.Replace("\r", Environment.NewLine).Replace("\n", Environment.NewLine).Split(Environment.NewLine).Last(line => !string.IsNullOrEmpty(line));
        var regex = new Regex(@"<!--(.*?)-->", RegexOptions.Singleline);
        var match = regex.Match(lastLineOfFile);
        if (!match.Success)
        {
            _logger.Information("No last line matched; content: {Content}", sevenDaysAgoContent);
            return null;
        }
        var content = match.Groups[1].Value;
        return JsonConvert.DeserializeObject<RunInfo>(content)!;
    }
    
    private static IList<BaseCheck.Deduction>? GetDeductions(ILogger logger, RunInfo? infoFromSevenDaysAgo, string fullPathToService, KeyValuePair<string, IList<BaseCheck.Deduction>> check)
    {
        var serviceScores = infoFromSevenDaysAgo?.ServiceScores;
        if (serviceScores == null || !serviceScores.ContainsKey(fullPathToService))
        {
            logger.Information("No info from 7 days ago for {Service}", fullPathToService);
            return null;
        }

        var deductionsByCheck = infoFromSevenDaysAgo?.ServiceScores[fullPathToService].DeductionsByCheck;
        if (deductionsByCheck == null || !deductionsByCheck.ContainsKey(check.Key))
        {
            logger.Information("No info from 7 days ago for {Service} and {Check}", fullPathToService, check.Key);
            return null;
        }

        return deductionsByCheck[check.Key];
    }

    private static string StyleOfNumber(int? score)
    {
        return $"color:{score switch
        {
            null => "rgba(var(--status-info-foreground),1)",
            >= 90 => "rgba(var(--palette-accent2),1)",
            >= 80 => "rgba(var(--palette-accent3),1)",
            >= 70 => "rgba(var(--status-warning-icon-foreground),1)",
            _ => "rgba(var(--palette-accent1),1)"
        }}";
    }

    private static string ColorizeAverageScore(int score)
    {
        return $"<span title style=\"{StyleOfNumber(score)}\">{score}</span>";
    }

    private static TableContent FormatJustifiedScore(IList<BaseCheck.Deduction> checkValue, IList<BaseCheck.Deduction>? sevenDaysAgo)
    {
        var finalScore = checkValue.CalculateFinalScore();
        var finalScoreSevenDaysAgo = sevenDaysAgo?.CalculateFinalScore();
        var delta = finalScore - finalScoreSevenDaysAgo;
        var deltaString = delta switch
        {
            null => "",
            0 => "",
            > 0 => $"<sub><span title=\"compared to 7 days ago\" style=\"color: rgba(var(--palette-accent2),1)\"> ↑+{delta}%</span></sub>",
            < 0 => $"<sub><span title=\"compared to 7 days ago\" style=\"color: rgba(var(--palette-accent1),1)\"> ↓{delta}%</span></sub>"
        };
        
        var justifications = string.Join("&#10;", checkValue.Select(deduction => deduction.ToString()));
        return new TableContent($"<span style=\"{StyleOfNumber(finalScore)}\">{(finalScore == null ? "n/a" : finalScore)}{deltaString}</span>", justifications);
    }

    private record TableContent(string Content, string Title, int Colspan = 1)
    { 
        public static implicit operator TableContent(string content) => new(content, "");
    }

    [GeneratedRegex("<noscript>.*</noscript>")]
    private static partial Regex NoScriptFilter();
}