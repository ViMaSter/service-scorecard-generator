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
        XDocument csproj;
        try
        {
            csproj = XDocument.Load(absolutePathToProjectFile);
        }
        catch (Exception e)
        {
            return new List<Deduction> { Deduction.Create(Logger, 100, "Couldn't parse {CsProj}: {Exception}", absolutePathToProjectFile, e) };
        }

        var hintPaths = csproj.Root!.Descendants("HintPath");
        return hintPaths.Select(hintPath => Deduction.Create(Logger, DeductionPerHintPath, "HintPath: {HintPath}", hintPath.Value)).ToList();
    }
}