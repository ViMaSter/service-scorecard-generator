package check_test

import (
	"os/exec"
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestNoDSStoreFixtures(t *testing.T) {
	testCases := []struct {
		name          string
		gitignore     *string
		dsStorePaths  []string
		trackDSStore  bool
		expectedCount int
		expectedScore *int
	}{
		{name: "NoDSStoreAndGloballyIgnored", gitignore: strPtr(".DS_Store\n"), dsStorePaths: nil, trackDSStore: false, expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "NoDSStoreButNotIgnored", gitignore: strPtr("bin/\n"), dsStorePaths: nil, trackDSStore: false, expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
		{name: "UntrackedDSStoreAndIgnored", gitignore: strPtr(".DS_Store\n"), dsStorePaths: []string{"src/.DS_Store"}, trackDSStore: false, expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "TrackedDSStoreAndIgnored", gitignore: strPtr(".DS_Store\n"), dsStorePaths: []string{"src/.DS_Store"}, trackDSStore: true, expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "TrackedDSStoreAndNotIgnored", gitignore: strPtr("bin/\n"), dsStorePaths: []string{"src/.DS_Store"}, trackDSStore: true, expectedCount: 2, expectedScore: testsupport.IntPtr(0)},
		{name: "MissingGitignore", gitignore: nil, dsStorePaths: nil, trackDSStore: false, expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
	}

	check := checks.NewNoDSStore()
	for _, testCase := range testCases {
		t.Run(testCase.name, func(t *testing.T) {
			if _, err := exec.LookPath("git"); err != nil {
				t.Skip("git is required for this test")
			}

			tempDir := t.TempDir()
			runGit(t, tempDir, "init")
			projectPath := filepath.Join(tempDir, "src", "project.csproj")
			testsupport.WriteFile(t, projectPath, "<Project></Project>")

			if testCase.gitignore != nil {
				testsupport.WriteFile(t, filepath.Join(tempDir, ".gitignore"), *testCase.gitignore)
			}
			for _, dsStorePath := range testCase.dsStorePaths {
				testsupport.WriteFile(t, filepath.Join(tempDir, filepath.FromSlash(dsStorePath)), "finder metadata")
				if testCase.trackDSStore {
					runGit(t, tempDir, "add", "-f", filepath.ToSlash(dsStorePath))
				}
			}

			testsupport.AssertFinalScore(t, check.Run(projectPath), testCase.expectedCount, testCase.expectedScore)
		})
	}
}

func runGit(t *testing.T, repoRoot string, args ...string) {
	t.Helper()
	command := exec.Command("git", append([]string{"-C", repoRoot}, args...)...)
	if output, err := command.CombinedOutput(); err != nil {
		t.Fatalf("git %v failed: %v\n%s", args, err, string(output))
	}
}