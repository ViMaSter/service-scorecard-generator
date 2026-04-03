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

func TestPendingRenovateAzurePRsHTTPSFixtures(t *testing.T) {
	basePath := filepath.ToSlash(filepath.Join("fixtures", "Checks", "PendingRenovateAzurePRs", "HTTPS"))
	client := &http.Client{Transport: roundTripperFunc(func(request *http.Request) (*http.Response, error) {
		directory := "azure"
		if strings.Contains(request.URL.Host, "visualstudio") {
			directory = "visualstudio"
		}
		fileName := filepath.Base(request.URL.Path)
		content := testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join(basePath, directory, fileName)))
		return &http.Response{StatusCode: 200, Status: "200 OK", Body: io.NopCloser(strings.NewReader(content)), Header: make(http.Header)}, nil
	})}
	check := checks.NewPendingRenovateAzurePRs("", client)
	testCases := []string{
		"git@ssh.dev.azure.com:v3/vimaster/ScorecardGenerator/TestService1",
		"vimaster@vs-ssh.visualstudio.com:v3/vimaster/ScorecardGenerator/TestService1",
		"https://dev.azure.com/vimaster/ScorecardGenerator/_git/TestService1",
		"https://vimaster.visualstudio.com/ScorecardGenerator/_git/TestService1",
	}
	for _, gitRepo := range testCases {
		t.Run(gitRepo, func(t *testing.T) {
			tempDirectory := t.TempDir()
			gitDirectory := filepath.Join(tempDirectory, ".git")
			testsupport.FixtureCopyTree(t, filepath.ToSlash(filepath.Join(basePath, "_git")), gitDirectory, func(relPath string, content string) string {
				if filepath.Base(relPath) == "config" {
					return strings.ReplaceAll(content, "{{URL}}", gitRepo)
				}
				return content
			})
			projectPath := filepath.Join(tempDirectory, "Cik.Magazine.CategoryService.csproj")
			testsupport.WriteFile(t, projectPath, `<Project Sdk="Microsoft.NET.Sdk"></Project>`)
			deductions := check.Run(projectPath)
			testsupport.AssertFinalScore(t, deductions, 1, testsupport.IntPtr(80))
		})
	}
	t.Run("no git path", func(t *testing.T) {
		projectPath := filepath.Join(t.TempDir(), "Cik.Magazine.CategoryService.csproj")
		testsupport.WriteFile(t, projectPath, `<Project Sdk="Microsoft.NET.Sdk"></Project>`)
		testsupport.AssertFinalScore(t, check.Run(projectPath), 1, testsupport.IntPtr(0))
	})
}

type roundTripperFunc func(*http.Request) (*http.Response, error)

func (f roundTripperFunc) RoundTrip(request *http.Request) (*http.Response, error) {
	return f(request)
}
