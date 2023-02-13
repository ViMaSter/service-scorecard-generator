using Newtonsoft.Json;
using ScorecardGenerator.Calculation;
using ScorecardGenerator.Checks;
using Serilog;

namespace ScorecardGenerator.Visualizer;

public class AzureWikiTableVisualizer : IVisualizer
{
    private readonly ILogger _logger;
    private readonly string _outputPath;
    private readonly string _dayOfGeneration;
    private const string QuestionMark = "<sup>&nbsp;<b><i><u>?</u></i></b></sup>";
    private const string AutoGenerationInfo = "<!-- !!! THIS FILE IS AUTOGENERATED - DO NOT EDIT IT MANUALLY !!! -->";
    private static readonly string AutoGenerationHeader = $"{AutoGenerationInfo}{Environment.NewLine}{AutoGenerationInfo}{Environment.NewLine}{AutoGenerationInfo}{Environment.NewLine}{Environment.NewLine}";

    public AzureWikiTableVisualizer(ILogger logger, string outputPath)
    {
        _logger = logger.ForContext<AzureWikiTableVisualizer>();
        _outputPath = outputPath;
        _dayOfGeneration = $"{DateTime.Now:yyyy-MM-dd}";
    }
    
    public void Visualize(RunInfo runInfo)
    {
        CreateDirectoryForDay();
        GenerateCheckPages(runInfo);
        GenerateServiceOverview(runInfo);
    }

    private void CreateDirectoryForDay()
    {
        if (!Directory.Exists(Path.Join(_outputPath, _dayOfGeneration)))
        {
            Directory.CreateDirectory(Path.Join(_outputPath, _dayOfGeneration));
        }
    }
    
    private void WriteGeneratedOutput(string path, string content)
    {
        File.WriteAllText(Path.Join(_outputPath, path), $"{AutoGenerationHeader}{content}");
    }

    private void GenerateCheckPages(RunInfo runInfo)
    {
        foreach (var (checkName, infoPageContent) in runInfo.Checks.Values.SelectMany(checks => checks))
        {
            WriteGeneratedOutput($"{_dayOfGeneration}/{checkName}.md", infoPageContent);
        }
    }

    private void GenerateServiceOverview(RunInfo runInfo)
    {
        var usageGuide = $"Hover over columns to show full service paths and score justifications.  {Environment.NewLine}Information on how to reach 100 points for each score can be found in the sub-pages of this page.{Environment.NewLine}[[_TOSP_]]{Environment.NewLine}{Environment.NewLine}";
        
        var lastUpdatedAt = DateTime.Now;
        
        const string headerElement = "th";
        const string columnElement = "td";
        var alternateColorIndex = 1;
        string ToBackgroundColor()
        {
            ++alternateColorIndex;

            if (alternateColorIndex % 2 == 0)
            {
                return "background-color: rgba(0, 0, 0, 0.05);";
            }

            return "";
        }

        string StyleForElement(int colorIndex, string element)
        {
            var style = "";
            if (colorIndex <= 3)
            {
                style += "background-color: rgba(var(--palette-neutral-2),1);";
            }
            if (element == "th")
            {
                style += "position: sticky; top: -2px;";
            }
            if (colorIndex == 2)
            {
                style += "top: 2.6em;";
            }

            return $"style=\"{style}\"";
        }
        string ToElement(string element, IEnumerable<TableContent> columns)
        {
            return $"<tr style=\"{ToBackgroundColor()}\">{string.Join("", columns.Select(entry => $"<{element} {StyleForElement(alternateColorIndex, element)} colspan=\"{entry.Colspan}\">{entry.Content}</{element}>"))}</tr>";
        }

        var runInfoJSON = JsonConvert.SerializeObject(runInfo);
        
        var headers = ToElement(headerElement, runInfo.Checks.Values.SelectMany(checksInGroup=>checksInGroup).Select(check => new TableContent(check.Name)).Prepend("ServiceName").Append("Average"));
        var groupData = ToElement(headerElement, runInfo.Checks.Where(group => group.Value.Any()).Select(group=>new TableContent(group.Key, group.Value.Count)).Prepend("   ").Append("   "));
        
        var output = runInfo.ServiceScores.Select(pair =>
        {
            var (fullPathToService, (scoreByCheckName, average)) = pair;
            var serviceName = $"<span title=\"{fullPathToService}\">{Path.GetFileNameWithoutExtension(fullPathToService)}{QuestionMark}</span>";
            return ToElement(columnElement, scoreByCheckName.Select(check => new TableContent(FormatJustifiedScore(check.Value))).Prepend(serviceName).Append(ColorizeAverageScore(average)));
        });
        
        _logger.Information("Generated scorecard at {LastUpdatedAt}", lastUpdatedAt);

        var headline = $"# Service Scorecard for {_dayOfGeneration}";
        WriteGeneratedOutput($"{_dayOfGeneration}.md", $"{headline}{Environment.NewLine}{Environment.NewLine}{usageGuide}{Environment.NewLine}{Environment.NewLine}<table style=\"height: 40vh\">{string.Join(Environment.NewLine, output.Prepend(headers).Prepend(groupData).Prepend(""))}</table>{Environment.NewLine}{Environment.NewLine}<!-- {runInfoJSON} -->");
    }

    private static string StyleOfNumber(int score)
    {
        return $"color:{score switch
        {
            >= 90 => "rgba(var(--palette-accent2),1",
            >= 80 => "rgba(var(--palette-accent3),1)",
            >= 70 => "var(--status-warning-icon-foreground)",
            _ => "rgba(var(--palette-accent1),1)"
        }}";
    }

    private static string ColorizeAverageScore(int score)
    {
        return $"<span title style=\"{StyleOfNumber(score)}\">{score}</span>";
    }

    private static string FormatJustifiedScore(IList<BaseCheck.Deduction> checkValue)
    {
        var finalScore = checkValue.CalculateFinalScore();
        var justifications = string.Join("&#10;", checkValue.Select(deduction => $"-{deduction.Score} points: {deduction.Justification}"));
        return $"<span title=\"{justifications}\" style=\"{StyleOfNumber(finalScore)}\">{finalScore}{(finalScore != 100 ? QuestionMark : "")}</span>";
    }

    private record TableContent(string Content, int Colspan = 1)
    { 
        public static implicit operator TableContent(string content) => new(content);
    }
}