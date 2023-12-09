namespace ScorecardGenerator.Visualizer;

internal interface IVisualizer
{
    string Visualize(Calculation.RunInfo runInfo);
}