package check_test

import (
	"os"
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestCodeOwnersFixtures(t *testing.T) {
	testCases := []struct {
		name           string
		codeowners     *string // nil means no CODEOWNERS file
		expectedCount  int
		expectedScore  *int
	}{
		{name: "NoCodeOwnersFile", codeowners: nil, expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "EmptyCodeOwners", codeowners: strPtr(""), expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "OnlyComments", codeowners: strPtr("# This is a comment\n# Another comment\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "OneOwner", codeowners: strPtr("* @owner1\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
		{name: "TwoOwners", codeowners: strPtr("* @owner1 @owner2\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "TwoOwnersOnSeparateLines", codeowners: strPtr("*.go @owner1\n*.js @owner2\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "DuplicateOwnerCountsOnce", codeowners: strPtr("*.go @owner1\n*.js @owner1\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
		{name: "ThreeOwners", codeowners: strPtr("* @owner1 @owner2 @owner3\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
	}
	check := checks.NewCodeOwners()
	for _, testCase := range testCases {
		t.Run(testCase.name, func(t *testing.T) {
			tempDir := t.TempDir()
			// Create .git directory to mark repo root
			if err := os.Mkdir(filepath.Join(tempDir, ".git"), 0o755); err != nil {
				t.Fatal(err)
			}
			// Create project file
			projectPath := filepath.Join(tempDir, "src", "project.csproj")
			testsupport.WriteFile(t, projectPath, "<Project></Project>")
			// Create CODEOWNERS if specified
			if testCase.codeowners != nil {
				testsupport.WriteFile(t, filepath.Join(tempDir, "CODEOWNERS"), *testCase.codeowners)
			}
			testsupport.AssertFinalScore(t, check.Run(projectPath), testCase.expectedCount, testCase.expectedScore)
		})
	}
}

func strPtr(s string) *string {
	return &s
}
