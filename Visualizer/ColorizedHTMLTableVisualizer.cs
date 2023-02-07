using Serilog;

namespace ScorecardGenerator.Visualizer;

internal class ColorizedHTMLTableVisualizer
{
    private readonly ILogger _logger;

    public ColorizedHTMLTableVisualizer(ILogger logger)
    {
        _logger = logger;
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
        
        const string headerElement = "th";
        const string columnElement = "td";
        string ToElement(string element, IEnumerable<TableContent> columns) => $"<tr>{string.Join("", columns.Select(entry => $"<{element} colspan=\"{entry.Colspan}\">{entry.Content}</{element}>"))}</tr>";
        
        var groupData = ToElement(headerElement, runInfo.Checks.Where(group => group.Value.Any()).Select(group=>new TableContent(group.Key, group.Value.Count)).Prepend("   ").Append("   "));
        var headers = ToElement(headerElement, runInfo.Checks.Values.SelectMany(checksInGroup=>checksInGroup).Select(check => new TableContent(check)).Prepend("ServiceName").Append("Average"));
        var output = runInfo.ServiceScores.Select(pair =>
        {
            var (serviceName, (scoreByCheckName, average)) = pair;
            return ToElement(columnElement, scoreByCheckName.Select(check => new TableContent(ColorizeNumber(check.Value))).Prepend(serviceName).Append(ColorizeNumber(average)));
        });
        
        _logger.Information("Generated scorecard at {LastUpdatedAt}", lastUpdatedAt);
        
        return $"{generationInfo}<table>{string.Join(Environment.NewLine, output.Prepend(headers).Prepend(groupData).Prepend(""))}</table>";
    }

    private record TableContent(string Content, int Colspan = 1)
    { 
        public static implicit operator TableContent(string content) => new TableContent(content);
    }
}