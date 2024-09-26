namespace ScorecardGenerator.Checks.RemainingDependencyUpgrades.RepositoryInfo.GitHub.Models;

public record GitHubPullRequest
(
    int number,
    GitHubPullRequestHead head
);