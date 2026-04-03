package checks

import (
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"os/exec"
	"path/filepath"
	"strings"
	"sync"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type PendingRenovateAzurePRs struct {
	baseCheck
	client *http.Client
	pat    string
}

type pullRequestsResponse struct {
	Value []struct {
		PullRequestID int    `json:"pullRequestId"`
		Title         string `json:"title"`
		SourceRefName string `json:"sourceRefName"`
		Repository    struct {
			ID   string `json:"id"`
			Name string `json:"name"`
		} `json:"repository"`
	} `json:"value"`
}

type iterationsResponse struct {
	Value []struct {
		ID int `json:"id"`
	} `json:"value"`
}

type changesResponse struct {
	ChangeEntries []struct {
		Item struct {
			Path string `json:"path"`
		} `json:"item"`
	} `json:"changeEntries"`
}

var pendingRenovateCache sync.Map

func NewPendingRenovateAzurePRs(azurePAT string, client *http.Client) *PendingRenovateAzurePRs {
	if client == nil {
		client = &http.Client{Timeout: 30 * time.Second}
	}
	return &PendingRenovateAzurePRs{baseCheck: newBaseCheck("PendingRenovateAzurePRs"), client: client, pat: azurePAT}
}

func (c *PendingRenovateAzurePRs) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	serviceRootDirectory := filepath.Dir(absolutePathToProjectFile)
	command := exec.Command("git", "remote", "-v")
	command.Dir = serviceRootDirectory
	stdout, _ := command.Output()
	azureInfo := parseAzureRemoteInfo(string(stdout))
	if len(azureInfo) == 0 {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No Azure DevOps remotes found for %v; can't check for open pull requests", serviceRootDirectory)}
	}
	selected := azureInfo[0]
	projectPullRequestsURL := fmt.Sprintf("https://dev.azure.com/%s/%s/_apis/git/pullrequests?api-version=7.0&searchCriteria.status=active", selected.organization, selected.project)
	var pullRequests pullRequestsResponse
	if err := c.getJSON(projectPullRequestsURL, &pullRequests); err != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No Azure DevOps remotes found for %v; can't check for open pull requests", serviceRootDirectory)}
	}
	projectFileNameWithExtension := filepath.Base(absolutePathToProjectFile)
	deductions := make([]scorecard.Deduction, 0)
	for _, pullRequest := range pullRequests.Value {
		if pullRequest.Repository.Name != selected.repo || !strings.Contains(pullRequest.SourceRefName, "renovate") {
			continue
		}
		allFilesChanged := make([]string, 0)
		iterationsURL := fmt.Sprintf("https://dev.azure.com/%s/%s/_apis/git/repositories/%s/pullRequests/%d/iterations?api-version=7.0", selected.organization, selected.project, pullRequest.Repository.ID, pullRequest.PullRequestID)
		var iterations iterationsResponse
		if err := c.getJSON(iterationsURL, &iterations); err != nil {
			continue
		}
		for _, iteration := range iterations.Value {
			changesURL := fmt.Sprintf("https://dev.azure.com/%s/%s/_apis/git/repositories/%s/pullRequests/%d/iterations/%d/changes?api-version=7.0", selected.organization, selected.project, pullRequest.Repository.ID, pullRequest.PullRequestID, iteration.ID)
			var changes changesResponse
			if err := c.getJSON(changesURL, &changes); err != nil {
				continue
			}
			for _, change := range changes.ChangeEntries {
				allFilesChanged = append(allFilesChanged, change.Item.Path)
			}
		}
		matched := false
		for _, changedFile := range allFilesChanged {
			if strings.HasSuffix(changedFile, projectFileNameWithExtension) {
				matched = true
				break
			}
		}
		if matched {
			deductions = append(deductions, scorecard.NewDeduction(20, "PR %v - %v", pullRequest.PullRequestID, pullRequest.Title))
		}
	}
	return deductions
}

func (c *PendingRenovateAzurePRs) getJSON(url string, target any) error {
	if cached, ok := pendingRenovateCache.Load(url); ok {
		return json.Unmarshal([]byte(cached.(string)), target)
	}
	var body []byte
	var lastErr error
	for _, wait := range []time.Duration{0, time.Second, time.Second, 3 * time.Second, 10 * time.Second} {
		if wait > 0 {
			time.Sleep(wait)
		}
		request, err := http.NewRequest(http.MethodGet, url, nil)
		if err != nil {
			lastErr = err
			continue
		}
		request.Header.Set("Authorization", "Basic "+base64.StdEncoding.EncodeToString([]byte(":"+c.pat)))
		response, err := c.client.Do(request)
		if err != nil {
			lastErr = err
			continue
		}
		body, err = ioReadAllAndClose(response)
		if err != nil {
			lastErr = err
			continue
		}
		if response.StatusCode >= 200 && response.StatusCode < 300 {
			pendingRenovateCache.Store(url, string(body))
			return json.Unmarshal(body, target)
		}
		lastErr = fmt.Errorf("unexpected status %s", response.Status)
	}
	return lastErr
}

func ioReadAllAndClose(response *http.Response) ([]byte, error) {
	defer response.Body.Close()
	return io.ReadAll(response.Body)
}

type azureRemote struct {
	organization string
	project      string
	repo         string
}

func parseAzureRemoteInfo(allLines string) []azureRemote {
	results := make([]azureRemote, 0)
	for _, line := range strings.Split(strings.ReplaceAll(strings.ReplaceAll(allLines, "\r\n", "\n"), "\r", "\n"), "\n") {
		if !strings.Contains(line, "visualstudio") && !strings.Contains(line, "dev.azure") {
			continue
		}
		line = strings.ReplaceAll(line, "\t", " ")
		for strings.Contains(line, "  ") {
			line = strings.ReplaceAll(line, "  ", " ")
		}
		parts := strings.Split(line, " ")
		if len(parts) < 2 {
			continue
		}
		remote := parts[1]
		if !strings.Contains(remote, "http") {
			remoteParts := strings.Split(remote, "/")
			if len(remoteParts) >= 3 {
				results = append(results, azureRemote{organization: remoteParts[len(remoteParts)-3], project: remoteParts[len(remoteParts)-2], repo: remoteParts[len(remoteParts)-1]})
			}
			continue
		}
		pathSplit := strings.Split(remote, "/")
		gitIndex := -1
		for index, part := range pathSplit {
			if part == "_git" {
				gitIndex = index
				break
			}
		}
		if gitIndex < 0 || gitIndex+1 >= len(pathSplit) {
			continue
		}
		if strings.Contains(remote, "dev.azure") && gitIndex >= 2 {
			results = append(results, azureRemote{organization: pathSplit[gitIndex-2], project: pathSplit[gitIndex-1], repo: pathSplit[gitIndex+1]})
			continue
		}
		if gitIndex >= 1 {
			hostParts := strings.Split(strings.TrimPrefix(pathSplit[2], "https://"), ".")
			organization := hostParts[0]
			results = append(results, azureRemote{organization: organization, project: pathSplit[gitIndex-1], repo: pathSplit[gitIndex+1]})
		}
	}
	return results
}

var _ Check = (*PendingRenovateAzurePRs)(nil)
