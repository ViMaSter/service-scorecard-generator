namespace ScorecardGenerator;

internal interface IRunCheck
{
    int Run(string workingDirectory, string relativePathToServiceRoot);
}