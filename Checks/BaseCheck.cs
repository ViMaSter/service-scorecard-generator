using System.Reflection;
using Serilog;

namespace ScorecardGenerator.Checks;

internal abstract class BaseCheck
{
    protected ILogger Logger;

    protected BaseCheck(ILogger logger)
    {
        Logger = logger;
        InfoPageContent = GenerateInfoPage();
    }

    public string InfoPageContent { get; }

    private string GenerateInfoPage()
    {
        var embeddedPathToInfoMD = $"{GetType().Namespace}.info.md";
        var exists = Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(embeddedPathToInfoMD);
        if (!exists)
        {
            throw new NotImplementedException($"Check {GetType().Namespace} implementing BaseCheck must have an `info.md` file with Build Action 'Embedded Resource' next to it");
        }
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedPathToInfoMD);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
    
    public int SetupLoggerAndRun(string workingDirectory, string relativePathToServiceRoot)
    {
        Logger = Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{GetType().FullName!.Split(".")[^2]}::{relativePathToServiceRoot}");
        Logger.Information("Running check");
        return Run(workingDirectory, relativePathToServiceRoot);
    }
    protected abstract int Run(string workingDirectory, string relativePathToServiceRoot);
}