using System.Xml.Linq;
using Serilog;

namespace ScorecardGenerator.Checks.HintPathCounter;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }
    
    const int DeductionPerHintPath = 10;

    protected override List<Deduction> Run(string absolutePathToProjectFile)
    {
        var csproj = XDocument.Load(absolutePathToProjectFile);
        if (csproj.Root == null)
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Couldn't parse {CsProj}", absolutePathToProjectFile) };
        }

        var hintPaths = csproj.Root.Descendants("HintPath");
        return hintPaths.Select(hintPath => Deduction.Create(Logger, DeductionPerHintPath, "HintPath: {HintPath}", hintPath.Value)).ToList();
    }
}