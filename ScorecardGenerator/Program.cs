using System.Diagnostics.CodeAnalysis;
using Cocona;
using Serilog;

namespace ScorecardGenerator;

[ExcludeFromCodeCoverage]
internal abstract class Program
{
    public static async Task Main()
    {
        var logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        var inst = new GenerateScorecard(logger);
        var app = CoconaLiteApp.Create();
        app.AddCommand(inst.Execute);
        await app.RunAsync();
    }
}