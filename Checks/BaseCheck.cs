using Serilog;

namespace ScorecardGenerator.Checks;

internal abstract class BaseCheck
{
    protected ILogger Logger;

    protected BaseCheck(ILogger logger)
    {
        Logger = logger;
    }
    public int SetupLoggerAndRun(string workingDirectory, string relativePathToServiceRoot)
    {
        Logger = Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{GetType().FullName!.Split(".")[^2]}::{relativePathToServiceRoot}");
        Logger.Information("Running check");
        return Run(workingDirectory, relativePathToServiceRoot);
    }
    protected abstract int Run(string workingDirectory, string relativePathToServiceRoot);
}