using System.Reflection;
using System.Text;
using Newtonsoft.Json;
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
        const string MARKDOWN_FILE_NAME = "README.md";
        var embeddedPathToInfoMD = $"{GetType().Namespace}.{MARKDOWN_FILE_NAME}";
        var exists = Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(embeddedPathToInfoMD);
        if (!exists)
        {
            throw new FileNotFoundException($"Check {GetType().Namespace} implementing BaseCheck must have an `{MARKDOWN_FILE_NAME}` file with Build Action 'Embedded Resource' next to it");
        }
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedPathToInfoMD);
        using var reader = new StreamReader(stream!);
        return reader.ReadToEnd();
    }
    
    public IList<Deduction> SetupLoggerAndRun(string absolutePathToProjectFile)
    {
        Logger = Logger.ForContext(Constants.SourceContextPropertyName, $"{GetType().FullName!.Split(".")[^2]}::{absolutePathToProjectFile.Replace(Directory.GetCurrentDirectory(), "")}");
        Logger.Information("Running check");
        return Run(absolutePathToProjectFile);
    }
    protected abstract IList<Deduction> Run(string absolutePathToProjectFile);

    public class Deduction
    {
        [JsonConstructor]
        private Deduction(int? score, string justification)
        {
            Score = score;
            Justification = justification;
        }

        public override string ToString()
        {
            if (IsDisqualification)
            {
                return $"disqualified: {Justification}";
            }
            
            return $"-{Score} points: {Justification}";
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
            
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem - justificationTemplate is inserted to be formatted with propertyValues; while not compile-time constant, it's not an interpolated string
            logger.Warning(justificationTemplate, propertyValues);
            return new Deduction(score, string.Format(netStyle, propertyValues));
        }

        [MessageTemplateFormatMethod(nameof(justificationTemplate))]
        public static Deduction CreateDisqualification(ILogger logger, string justificationTemplate, params object[] propertyValues)
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
            
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem - justificationTemplate is inserted to be formatted with propertyValues; while not compile-time constant, it's not an interpolated string
            logger.Warning(justificationTemplate, propertyValues);
            return new Deduction(null, string.Format(netStyle, propertyValues));
        }
        
        public string Justification { get; }

        public int? Score { get; }
        public bool IsDisqualification => Score == null;
    }
}