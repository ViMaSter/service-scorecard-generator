package app

import (
	"encoding/json"
	"fmt"
	"net/http"
	"os"
	"path/filepath"
	"sort"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/internal/resources"
	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/visualizer"
)

type Generator struct {
	Now        func() time.Time
	HTTPClient *http.Client
	WorkingDir string
}

type configuredChecks struct {
	Checks struct {
		Gold   []string `json:"Gold"`
		Silver []string `json:"Silver"`
		Bronze []string `json:"Bronze"`
	} `json:"Checks"`
}

func (g Generator) Execute(outputPath string, visualizerName string, excludePath string, azurePAT string) error {
	workingDir := g.WorkingDir
	if workingDir == "" {
		currentDirectory, err := os.Getwd()
		if err != nil {
			return err
		}
		workingDir = currentDirectory
	}
	registry := checks.Registry(checks.RegistryConfig{AzurePAT: azurePAT, Client: g.HTTPClient})
	groups, err := loadConfiguredChecks(workingDir, registry)
	if err != nil {
		return err
	}
	projects, err := findProjects(workingDir, excludePath)
	if err != nil {
		return err
	}
	services := make([]scorecard.ServiceScore, 0, len(projects))
	for _, project := range projects {
		goldChecks := runChecks(groups.GroupByName("Gold").Checks, project)
		silverChecks := runChecks(groups.GroupByName("Silver").Checks, project)
		bronzeChecks := runChecks(groups.GroupByName("Bronze").Checks, project)
		orderedChecks := append(append(goldChecks, silverChecks...), bronzeChecks...)
		servicePath := filepath.ToSlash(strings.TrimPrefix(project, workingDir))
		services = append(services, scorecard.ServiceScore{Path: servicePath, Score: scorecard.ServiceScorecard{Checks: orderedChecks, Average: scorecard.CalculateAverage(goldChecks, silverChecks, bronzeChecks)}})
	}
	runInfo := scorecard.RunInfo{Groups: groups.toScorecardGroups(), Services: services}
	visualizerToUse, err := selectVisualizer(visualizerName, outputPath, g.Now)
	if err != nil {
		return err
	}
	return visualizerToUse.Visualize(runInfo)
}

func runChecks(checkInfos []runtimeCheckInfo, project string) []scorecard.CheckResult {
	results := make([]scorecard.CheckResult, 0, len(checkInfos))
	for _, checkInfo := range checkInfos {
		results = append(results, scorecard.CheckResult{Name: checkInfo.Name, Deductions: checkInfo.Runtime.Run(project)})
	}
	return results
}

type runtimeCheckInfo struct {
	scorecard.CheckInfo
	Runtime checks.Check
}

type runtimeGroup struct {
	Name   string
	Checks []runtimeCheckInfo
}

type runtimeRunInfo struct {
	Groups []runtimeGroup
}

func (r runtimeRunInfo) toScorecardGroups() []scorecard.Group {
	groups := make([]scorecard.Group, 0, len(r.Groups))
	for _, group := range r.Groups {
		checkInfos := make([]scorecard.CheckInfo, 0, len(group.Checks))
		for _, check := range group.Checks {
			checkInfos = append(checkInfos, check.CheckInfo)
		}
		groups = append(groups, scorecard.Group{Name: group.Name, Checks: checkInfos})
	}
	return groups
}

func (r runtimeRunInfo) GroupByName(name string) runtimeGroup {
	for _, group := range r.Groups {
		if group.Name == name {
			return group
		}
	}
	return runtimeGroup{Name: name}
}

func loadConfiguredChecks(workingDir string, registry map[string]func() checks.Check) (runtimeRunInfo, error) {
	configPath := filepath.Join(workingDir, "scorecard.config.json")
	if _, err := os.Stat(configPath); os.IsNotExist(err) {
		if err := os.WriteFile(configPath, []byte(resources.DefaultConfigJSON), 0o644); err != nil {
			return runtimeRunInfo{}, err
		}
	}
	content, err := os.ReadFile(configPath)
	if err != nil {
		return runtimeRunInfo{}, err
	}
	var configured configuredChecks
	if err := json.Unmarshal(content, &configured); err != nil {
		return runtimeRunInfo{}, err
	}
	result := runtimeRunInfo{Groups: make([]runtimeGroup, 0, len(scorecard.GroupOrder))}
	for _, groupName := range scorecard.GroupOrder {
		names := configured.Checks.Gold
		switch groupName {
		case "Silver":
			names = configured.Checks.Silver
		case "Bronze":
			names = configured.Checks.Bronze
		}
		group := runtimeGroup{Name: groupName, Checks: make([]runtimeCheckInfo, 0, len(names))}
		for _, name := range names {
			factory, ok := registry[name]
			if !ok {
				available := make([]string, 0, len(registry))
				for availableName := range registry {
					available = append(available, "ScorecardGenerator.Checks."+availableName)
				}
				sort.Strings(available)
				return runtimeRunInfo{}, fmt.Errorf("Configuration inside scorecard.config.json is invalid: Could not find check with name `%s`. List of currently available checks: \n%s", name, strings.Join(available, "\n"))
			}
			check := factory()
			group.Checks = append(group.Checks, runtimeCheckInfo{CheckInfo: scorecard.CheckInfo{Name: check.Name(), InfoPageContent: check.InfoPageContent()}, Runtime: check})
		}
		result.Groups = append(result.Groups, group)
	}
	return result, nil
}

func findProjects(workingDir string, excludePath string) ([]string, error) {
	projects := make([]string, 0)
	err := filepath.WalkDir(workingDir, func(path string, entry os.DirEntry, err error) error {
		if err != nil {
			return err
		}
		if entry.IsDir() {
			if entry.Name() == ".git" {
				return filepath.SkipDir
			}
			return nil
		}
		if filepath.Ext(path) != ".csproj" {
			return nil
		}
		if excludePath != "" && strings.Contains(path, excludePath) {
			return nil
		}
		projects = append(projects, path)
		return nil
	})
	if err != nil {
		return nil, err
	}
	sort.Strings(projects)
	return projects, nil
}

func selectVisualizer(name string, outputPath string, now func() time.Time) (visualizer.Visualizer, error) {
	switch name {
	case "html":
		return visualizer.NewHTMLVisualizer(outputPath, now), nil
	case "azurewiki":
		return visualizer.NewAzureWikiTableVisualizer(outputPath, now), nil
	case "gitlabmarkdown":
		return visualizer.NewGitLabMarkdownVisualizer(outputPath, now), nil
	case "mkdocsmarkdown":
		return visualizer.NewMkDocsMarkdownVisualizer(outputPath, now), nil
	default:
		return nil, fmt.Errorf("Unknown visualizer %s", name)
	}
}

func (g Generator) ListChecks(azurePAT string) (string, error) {
	registry := checks.Registry(checks.RegistryConfig{AzurePAT: azurePAT, Client: g.HTTPClient})
	availableChecks := make([]string, 0, len(registry))
	for name := range registry {
		availableChecks = append(availableChecks, name)
	}
	sort.Strings(availableChecks)
	for index, name := range availableChecks {
		availableChecks[index] = "\n  - " + name
	}
	return "Available checks:" + strings.Join(availableChecks, ""), nil
}
