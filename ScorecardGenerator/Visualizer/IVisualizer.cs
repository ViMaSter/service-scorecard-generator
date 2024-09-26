using ScorecardGenerator.Calculation;

namespace ScorecardGenerator.Visualizer;

internal interface IVisualizer
{
    string Visualize(RunInfo runInfo);
}