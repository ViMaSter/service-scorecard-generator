package check_test

import (
	"io"
	"net/http"
	"path/filepath"
	"strings"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestPendingRenovateGitLabPRsReturnsDeductionsForOpenedRenovateMergeRequests(t *testing.T) {
	client := &http.Client{Transport: roundTripperFunc(func(request *http.Request) (*http.Response, error) {
		if got := request.Header.Get("PRIVATE-TOKEN"); got != "test-pat" {
			t.Fatalf("expected PRIVATE-TOKEN header to be set, got %q", got)
		}
		if request.URL.Scheme != "https" || request.URL.Host != "gitlab.example.com" {
			t.Fatalf("unexpected request URL: %s", request.URL.String())
		}
		expectedPath := "/api/v4/projects/platform%2Fpayments%2Fservice-a/merge_requests"
		if request.URL.EscapedPath() != expectedPath {
			t.Fatalf("expected path %q, got %q", expectedPath, request.URL.EscapedPath())
		}
		body := `[
			{"iid": 17, "title": "chore(deps): update dependency dotnet to v9", "source_branch": "renovate/dotnet-9.x"},
			{"iid": 18, "title": "Fix deployment docs", "source_branch": "docs/fix-links"},
			{"iid": 19, "title": "Renovate digest updates", "source_branch": "deps/update-base-image"}
		]`
		return &http.Response{StatusCode: 200, Status: "200 OK", Body: io.NopCloser(strings.NewReader(body)), Header: make(http.Header)}, nil
	})}
	check := checks.NewPendingRenovateGitLabPRs("test-pat", client)
	repoRoot := writeGitLabRepo(t, "git@gitlab.example.com:platform/payments/service-a.git")
	deductions := check.Run(repoRoot)
	testsupport.AssertFinalScore(t, deductions, 2, testsupport.IntPtr(60))
}

func TestPendingRenovateGitLabPRsSupportsHTTPSRemotes(t *testing.T) {
	client := &http.Client{Transport: roundTripperFunc(func(request *http.Request) (*http.Response, error) {
		return &http.Response{StatusCode: 200, Status: "200 OK", Body: io.NopCloser(strings.NewReader("[]")), Header: make(http.Header)}, nil
	})}
	check := checks.NewPendingRenovateGitLabPRs("test-pat", client)
	repoRoot := writeGitLabRepo(t, "https://gitlab.example.com/platform/payments/service-a.git")
	testsupport.AssertFinalScore(t, check.Run(repoRoot), 0, testsupport.IntPtr(100))
}

func TestPendingRenovateGitLabPRsRequiresPAT(t *testing.T) {
	check := checks.NewPendingRenovateGitLabPRs("", &http.Client{Transport: roundTripperFunc(func(request *http.Request) (*http.Response, error) {
		t.Fatal("unexpected HTTP request without PAT")
		return nil, nil
	})})
	repoRoot := writeGitLabRepo(t, "git@gitlab.example.com:platform/payments/service-a.git")
	testsupport.AssertFinalScore(t, check.Run(repoRoot), 1, testsupport.IntPtr(0))
}

func TestPendingRenovateGitLabPRsRequiresOriginRemote(t *testing.T) {
	check := checks.NewPendingRenovateGitLabPRs("test-pat", &http.Client{Transport: roundTripperFunc(func(request *http.Request) (*http.Response, error) {
		t.Fatal("unexpected HTTP request without origin remote")
		return nil, nil
	})})
	repoRoot := t.TempDir()
	testsupport.WriteFile(t, filepath.Join(repoRoot, ".git", "config"), "[core]\n\trepositoryformatversion = 0\n")
	testsupport.AssertFinalScore(t, check.Run(repoRoot), 1, testsupport.IntPtr(0))
}

func writeGitLabRepo(t *testing.T, remoteURL string) string {
	t.Helper()
	repoRoot := t.TempDir()
	testsupport.WriteFile(t, filepath.Join(repoRoot, ".git", "config"), strings.Join([]string{
		"[core]",
		"\trepositoryformatversion = 0",
		"[remote \"origin\"]",
		"\turl = " + remoteURL,
		"\tfetch = +refs/heads/*:refs/remotes/origin/*",
	}, "\n"))
	return repoRoot
}