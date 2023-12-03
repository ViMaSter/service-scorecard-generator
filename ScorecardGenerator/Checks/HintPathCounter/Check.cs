using System.Xml.Linq;
using Serilog;

namespace ScorecardGenerator.Checks.HintPathCounter;

public class Check : BaseCheck
{
    public Check(ILogger logger) : base(logger)
    {
    }

    private const int DEDUCTION_PER_HINT_PATH = 10;

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
        return hintPaths.Select(hintPath => Deduction.Create(Logger, DEDUCTION_PER_HINT_PATH, "HintPath: {HintPath}", hintPath.Value)).ToList();
    }
}