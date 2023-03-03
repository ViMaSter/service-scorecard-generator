using System.Globalization;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ScorecardGenerator.Checks.SonarQubeCoverage;

public class Check : BaseCheck
{
    private readonly string _sonarQubeToken;

    public Check(ILogger logger, string sonarQubeToken) : base(logger)
    {
        _sonarQubeToken = sonarQubeToken;
    }

    protected override IList<Deduction> Run(string absolutePathToProjectFile)
    {
        var projectKey = string.Join(".", Path.GetDirectoryName(absolutePathToProjectFile)!.Split(Path.DirectorySeparatorChar).TakeLast(3));

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sonarQubeToken);
        var response = client.GetAsync($"https://sonarcloud.io/api/measures/component?component={projectKey}&metricKeys=coverage").Result;
        if (!response.IsSuccessStatusCode)
        {
            return new List<Deduction>
            {
                Deduction.Create(Logger, 100, "SonarQube coverage check failed with status code {StatusCode} for project key {ProjectKey}", response.StatusCode, projectKey)
            };
        }
        var responseText = response.Content.ReadAsStringAsync().Result;
        var json = JsonConvert.DeserializeObject<dynamic>(responseText);
        if (json == null)
        {
            return HandleUnexpectedFormat(projectKey, responseText, nameof(responseText));
        }

        var measures = (JArray)json.component.measures;
        var coverage = measures.FirstOrDefault((dynamic entry)=> entry.metric.Value == "coverage")?.value;
        
        if (coverage == null)
        {
            return HandleUnexpectedFormat(projectKey, responseText, nameof(coverage));
        }

        string coverageAsString = coverage;
        if (string.IsNullOrEmpty(coverageAsString))
        { 
            return HandleUnexpectedFormat(projectKey, responseText, nameof(coverageAsString));
        }
        
        if (!decimal.TryParse(coverageAsString, NumberStyles.Any, CultureInfo.InvariantCulture, out var coverageAsDecimal))
        {
            return HandleUnexpectedFormat(projectKey, responseText, nameof(coverageAsDecimal));
        }
        
        return new List<Deduction>
        {
            Deduction.Create(Logger, (int)Math.Ceiling(100 - coverageAsDecimal), "Coverage for project key {ProjectKey} of {Coverage}%; rounding up and removing one point per missing percent of coverage", projectKey, coverageAsDecimal)
        };
    }

    private IList<Deduction> HandleUnexpectedFormat(string projectKey, string content, string parsingStep)
    {
        return new List<Deduction>
        {
            Deduction.Create(Logger, 100, "SonarQube coverage check failed with unexpected response at step '{Step}' for project key {ProjectKey}: {Content}", parsingStep, projectKey, content)
        };
    }
}