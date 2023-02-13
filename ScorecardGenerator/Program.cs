using Cocona;
using ScorecardGenerator;
using Serilog;

var logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}][{SourceContext}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var inst = new GenerateScorecard(logger);
var app = CoconaLiteApp.Create();
app.AddCommand(inst.Execute);
await app.RunAsync();