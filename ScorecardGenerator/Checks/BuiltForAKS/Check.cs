using Serilog;

namespace ScorecardGenerator.Checks.BuiltForAKS;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    private readonly string[] _disqualificationKeywords = { "Test", "Common", "Shared" };
    
    protected override IList<Deduction> Run(string workingDirectory, string relativePathToServiceRoot)
    {
        var absolutePathToServiceRoot = Path.Join(workingDirectory, relativePathToServiceRoot);
        var disqualificationMatch = _disqualificationKeywords.FirstOrDefault(keyword => relativePathToServiceRoot.Contains($".{keyword}"));
        if (!string.IsNullOrEmpty(disqualificationMatch))
        {
            return new List<Deduction>() {Deduction.CreateDisqualification(Logger, "Skipping projects containing '.{match}' in filename", disqualificationMatch)};
        }
        var pipelineFiles = Directory.GetFiles(absolutePathToServiceRoot, "*.yml", SearchOption.TopDirectoryOnly);
        if (!pipelineFiles.Any())
        {
            return new List<Deduction>(new[] { Deduction.Create(Logger, 100, "No .yml file found next to to .csproj file") });
        }

        var firstPath = pipelineFiles.First();
        if (pipelineFiles.Length > 1)
        {
            return pipelineFiles.Select(path => Deduction.Create(Logger, 5, "More than one pipeline file: {Path}", path)).ToList();
        }

        if (firstPath.Contains("build-"))
        {
            return new List<Deduction>();    
        }

        if (firstPath.Contains("azure-"))
        {
            return new List<Deduction>();    
        }

        if (firstPath.Contains("onprem-"))
        {
            return new List<Deduction>(new[] { Deduction.Create(Logger, 100, "Service pipeline file doesn't start with 'azure-'; actual: {Actual}", firstPath) });
        }

        return new List<Deduction> { Deduction.Create(Logger, 5, ".yml needs to start with either 'azure-' for services or 'build-' for libraries; actual: '{Path}'", firstPath) };
    }
}
