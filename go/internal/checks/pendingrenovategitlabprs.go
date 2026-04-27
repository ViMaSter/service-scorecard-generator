package checks

import (
	"encoding/json"
	"fmt"
	"io"
	"net/http"
	"net/url"
	"os"
	"path/filepath"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type PendingRenovateGitLabMRs struct {
	checkruntime.BaseCheck
	client *http.Client
	pat    string
}

type gitLabRemote struct {
	apiBaseURL  string
	projectPath string
}

type gitLabMergeRequest struct {
	IID          int    `json:"iid"`
	Title        string `json:"title"`
	SourceBranch string `json:"source_branch"`
	WebURL       string `json:"web_url"`
}

func NewPendingRenovateGitLabMRs(pat string, client *http.Client) *PendingRenovateGitLabMRs {
	if client == nil {
		client = &http.Client{Timeout: 30 * time.Second}
	}
	return &PendingRenovateGitLabMRs{BaseCheck: checkruntime.NewBaseCheck("PendingRenovateGitLabMRs"), client: client, pat: pat}
}

func (c *PendingRenovateGitLabMRs) Run(path string) []scorecard.Deduction {
	repoRoot := findRepoRoot(path)
	if strings.TrimSpace(c.pat) == "" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No PAT provided for %v; can't check for open GitLab merge requests", checkruntime.RelPath(repoRoot))}
	}
	originURL, err := readGitRemoteOriginURL(repoRoot)
	if err != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No git remote origin found for %v; can't check for open GitLab merge requests", checkruntime.RelPath(repoRoot))}
	}
	remote, err := parseGitLabRemote(originURL)
	if err != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Could not parse GitLab remote origin for %v; can't check for open merge requests", checkruntime.RelPath(repoRoot))}
	}
	mergeRequests, err := c.listOpenMergeRequests(remote)
	if err != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Could not read open GitLab merge requests for %v", checkruntime.RelPath(repoRoot))}
	}
	deductions := make([]scorecard.Deduction, 0)
	for _, mergeRequest := range mergeRequests {
		if !isRenovateMergeRequest(mergeRequest) {
			continue
		}
		deductions = append(deductions, scorecard.NewDeduction(20, "MR !%v - %v", mergeRequest.IID, mergeRequest.Title))
	}
	return deductions
}

func readGitRemoteOriginURL(repoRoot string) (string, error) {
	content, err := os.ReadFile(filepath.Join(repoRoot, ".git", "config"))
	if err != nil {
		return "", err
	}
	inOriginSection := false
	for _, line := range strings.Split(strings.ReplaceAll(string(content), "\r\n", "\n"), "\n") {
		trimmed := strings.TrimSpace(line)
		if trimmed == "" || strings.HasPrefix(trimmed, "#") || strings.HasPrefix(trimmed, ";") {
			continue
		}
		if strings.HasPrefix(trimmed, "[") && strings.HasSuffix(trimmed, "]") {
			inOriginSection = trimmed == `[remote "origin"]`
			continue
		}
		if !inOriginSection {
			continue
		}
		key, value, found := strings.Cut(trimmed, "=")
		if found && strings.TrimSpace(key) == "url" {
			return strings.TrimSpace(value), nil
		}
	}
	return "", fmt.Errorf("remote.origin.url not found")
}

func parseGitLabRemote(remoteOriginURL string) (gitLabRemote, error) {
	trimmed := strings.TrimSpace(remoteOriginURL)
	if trimmed == "" {
		return gitLabRemote{}, fmt.Errorf("empty remote")
	}
	if strings.HasPrefix(trimmed, "http://") || strings.HasPrefix(trimmed, "https://") {
		parsed, err := url.Parse(trimmed)
		if err != nil {
			return gitLabRemote{}, err
		}
		projectPath := strings.Trim(strings.TrimSuffix(parsed.Path, ".git"), "/")
		if projectPath == "" {
			return gitLabRemote{}, fmt.Errorf("missing project path")
		}
		return gitLabRemote{apiBaseURL: fmt.Sprintf("%s://%s", parsed.Scheme, parsed.Host), projectPath: projectPath}, nil
	}
	if strings.HasPrefix(trimmed, "ssh://") {
		parsed, err := url.Parse(trimmed)
		if err != nil {
			return gitLabRemote{}, err
		}
		projectPath := strings.Trim(strings.TrimSuffix(parsed.Path, ".git"), "/")
		if parsed.Hostname() == "" || projectPath == "" {
			return gitLabRemote{}, fmt.Errorf("invalid ssh remote")
		}
		return gitLabRemote{apiBaseURL: fmt.Sprintf("https://%s", parsed.Hostname()), projectPath: projectPath}, nil
	}
	hostAndPath, found := strings.CutPrefix(trimmed, "git@")
	if !found {
		hostAndPath = trimmed
	}
	host, projectPath, found := strings.Cut(hostAndPath, ":")
	if !found || strings.TrimSpace(host) == "" {
		return gitLabRemote{}, fmt.Errorf("unsupported remote")
	}
	projectPath = strings.Trim(strings.TrimSuffix(projectPath, ".git"), "/")
	if projectPath == "" {
		return gitLabRemote{}, fmt.Errorf("missing project path")
	}
	return gitLabRemote{apiBaseURL: fmt.Sprintf("https://%s", strings.TrimSpace(host)), projectPath: projectPath}, nil
}

func isRenovateMergeRequest(mergeRequest gitLabMergeRequest) bool {
	branch := strings.ToLower(mergeRequest.SourceBranch)
	title := strings.ToLower(mergeRequest.Title)
	return strings.Contains(branch, "renovate") || strings.Contains(title, "renovate")
}

func (c *PendingRenovateGitLabMRs) listOpenMergeRequests(remote gitLabRemote) ([]gitLabMergeRequest, error) {
	endpoint := fmt.Sprintf("%s/api/v4/projects/%s/merge_requests?state=opened&per_page=100", remote.apiBaseURL, url.PathEscape(remote.projectPath))
	fmt.Printf("[%s INF][] Fetching GitLab merge request data for %s from %s\n", time.Now().Format("15:04:05"), remote.projectPath, remote.apiBaseURL)
	var mergeRequests []gitLabMergeRequest
	if err := c.getJSON(endpoint, &mergeRequests); err != nil {
		return nil, err
	}
	return mergeRequests, nil
}

func (c *PendingRenovateGitLabMRs) getJSON(url string, target any) error {
	request, err := http.NewRequest(http.MethodGet, url, nil)
	if err != nil {
		return err
	}
	request.Header.Set("PRIVATE-TOKEN", c.pat)
	response, err := c.client.Do(request)
	if err != nil {
		return err
	}
	body, err := ioReadAllAndCloseGitLab(response)
	if err != nil {
		return err
	}
	if response.StatusCode < 200 || response.StatusCode >= 300 {
		return fmt.Errorf("unexpected status %s", response.Status)
	}
	return json.Unmarshal(body, target)
}

func ioReadAllAndCloseGitLab(response *http.Response) ([]byte, error) {
	defer response.Body.Close()
	return io.ReadAll(response.Body)
}

var _ checkruntime.Check = (*PendingRenovateGitLabMRs)(nil)