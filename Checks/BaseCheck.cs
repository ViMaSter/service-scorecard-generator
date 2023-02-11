using System.Reflection;
using System.Text;
using Serilog;
using Serilog.Core;
using Serilog.Parsing;

namespace ScorecardGenerator.Checks;

public abstract class BaseCheck
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
    
    public IList<Deduction> SetupLoggerAndRun(string workingDirectory, string relativePathToServiceRoot)
    {
        Logger = Logger.ForContext(Serilog.Core.Constants.SourceContextPropertyName, $"{GetType().FullName!.Split(".")[^2]}::{relativePathToServiceRoot}");
        Logger.Information("Running check");
        return Run(workingDirectory, relativePathToServiceRoot);
    }
    protected abstract IList<Deduction> Run(string workingDirectory, string relativePathToServiceRoot);

    public class Deduction
    {
        private Deduction(int score, string justification)
        {
            this.Score = score;
            this.Justification = justification;
        }
        
        [MessageTemplateFormatMethod(nameof(justificationTemplate))]
        public static Deduction Create(ILogger logger, int score, string justificationTemplate, params object[] propertyValues)
        {
            var parser = new MessageTemplateParser();
            var template = parser.Parse(justificationTemplate);
            var format = new StringBuilder();
            var index = 0;
            foreach (var tok in template.Tokens)
            {
                if (tok is TextToken)
                    format.Append(tok);
                else
                    format.Append("{" + index++ + "}");
            }
            var netStyle = format.ToString();
            
            logger.Warning(justificationTemplate, propertyValues);
            return new Deduction(score, string.Format(netStyle, propertyValues));
        }
        
        public string Justification { get; }

        public int Score { get; }
    }
}