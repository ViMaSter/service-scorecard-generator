using Serilog;

namespace ScorecardGenerator.Visualizer;

internal class MarkdownVisualizer
{
    private readonly ILogger _logger;

    public MarkdownVisualizer(ILogger logger)
    {
        _logger = logger;
    }

    private static string ToProjectRow(KeyValuePair<string, Dictionary<string, int>> arg)
    {
        return $"| {arg.Key} | {string.Join(" | ", arg.Value.Values.Select(ColorizeNumber))} |";
    }

    private static string ColorizeNumber(int arg)
    {
        var color = arg switch
        {
            >= 90 => "green",
            >= 80 => "yellow",
            >= 70 => "orange",
            _ => "red"
        };
        return $"<span style=\"color:{color}\">{arg}</span>";
    }
    
    public string ToMarkdown(Calculation.RunInfo runInfo)
    {
        var lastUpdatedAt = DateTime.Now;
        var generationInfo = $"Scorecard generated at: {lastUpdatedAt:yyyy-MM-dd HH:mm:ss}";
        
        var headers = $"| ServiceName | {string.Join(" | ", runInfo.checks.Select(check => check.GetType().Name))} | Average |";
        var divider = $"| --- | {string.Join(" | ", runInfo.checks.Select(_ => "---"))} | --- |";
        return string.Join(Environment.NewLine, runInfo.serviceScores.Select(ToProjectRow).Prepend(divider).Prepend(headers).Prepend("").Prepend(generationInfo));
    }
}