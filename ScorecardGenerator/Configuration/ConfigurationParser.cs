using Newtonsoft.Json;
using ScorecardGenerator.Checks;
using ScorecardGenerator.Configuration.Models;
using Serilog;

namespace ScorecardGenerator.Configuration;

public class ConfigurationParser
{
    private readonly ILogger _logger;
    private readonly IEnumerable<object> _services;
    

    public ConfigurationParser(ILogger logger, IEnumerable<object> services)
    {
        _logger = logger;
        _services = services;
    }

    /// <summary>
    /// Constructs an instances of a check based on the name
    /// </summary>
    /// <remarks>
    /// This method uses reflection to find a class with the name of the check.
    /// Check classes must be in the namespace `ScorecardGenerator.Checks.{checkName}` and must inherit from `BaseCheck`.
    /// Check classes also must have a constructor that takes an `ILogger` as its first parameter.
    /// Remaining parameters must fulfill one of the following requirements:
    ///  - services that are registered in the dependency injection container `_services`
    ///  - optional parameters with default values
    /// </remarks>
    /// <param name="checkName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private BaseCheck CheckFromName(string checkName)
    {
        var assembly = typeof(BaseCheck).Assembly;
        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "Check" && t.BaseType == typeof(BaseCheck) && t.Namespace == $"ScorecardGenerator.Checks.{checkName}");
        if (type == null)
        {
            throw new ArgumentException($"Could not find check with name `{checkName}`. List of currently available checks: {Environment.NewLine}{string.Join(Environment.NewLine, assembly.GetTypes().Where(t => t.Name == "Check" && t.BaseType == typeof(BaseCheck)).Select(t => t.Namespace))}");
        }
        var constructor = type.GetConstructors().First(c => c.GetParameters().First().ParameterType == typeof(ILogger));
        var types = constructor.GetParameters().Skip(1).ToArray();
        var parameters = types
            .Where(t=> !t.IsOptional)
            .Select(t => _services.First(s => s.GetType() == t.ParameterType))
            .ToArray()
            .Prepend(_logger)
            .ToArray()
            .Concat(types.Where(t => t.IsOptional)
            .Select(t => t.DefaultValue))
            .ToArray();
        return (BaseCheck) Activator.CreateInstance(type, parameters)!;
    }
    public Models.Checks LoadChecks()
    {
        const string defaultJSON = "default.json";
        const string scorecardConfigJSON = "scorecard.config.json";
        var assembly = typeof(BaseCheck).Assembly;
        var resourceName = $"{typeof(ConfigurationParser).Namespace}.{defaultJSON}";
        var resourceStream = assembly.GetManifestResourceStream(resourceName)!;
        var workingDirectory = Directory.GetCurrentDirectory();
        var absolutePathToScorecardConfig = Path.Join(workingDirectory, scorecardConfigJSON);
        if (!File.Exists(absolutePathToScorecardConfig))
        {
            using var fileStream = File.Create(absolutePathToScorecardConfig);
            resourceStream.CopyTo(fileStream);
        }
        
        using var reader = new StreamReader(absolutePathToScorecardConfig);
        var json = reader.ReadToEnd();
        try
        {
            var data = JsonConvert.DeserializeObject<CheckData>(json)!.Checks;
            return new Models.Checks(
                data["Gold"].Select(CheckFromName).ToList(),
                data["Silver"].Select(CheckFromName).ToList(),
                data["Bronze"].Select(CheckFromName).ToList()
            );
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException($"Configuration inside {scorecardConfigJSON} is invalid: {e.Message}");
        }
    }
}