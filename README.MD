# Service Scorecard Generator
Go project which generates a scorecard for each repository in a GitLab group. Hardcoded to gitlab.kloudease.tech.

## Usage
```
just build
just test

just run <GITLAB_GROUP> <PAT>
```

Replace `<GITLAB_GROUP>` with the name of the GitLab group you want to generate scorecards for, and `<PAT>` with a Personal Access Token with these scopes:
- `read_repository`
- `write_repository`
- `api`
- `read_api`

Generates an mkdocs-compatible Markdown file by default inside `./run-$DATE-$TIME`. You can change the output format by changing the `VISUALIZER` environment variable:
- `html`
- `azurewiki`
- `gitlabmarkdown`
- `mkdocsmarkdown`