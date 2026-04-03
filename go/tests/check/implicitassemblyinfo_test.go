package check_test

import (
	"fmt"
	"path/filepath"
	"strings"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestImplicitAssemblyInfoFixtures(t *testing.T) {
	testCases := []struct {
		fixture       string
		expectedCount int
		expectedScore *int
	}{
		{fixture: "CompleteCsprojFile", expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{fixture: "DisabledGenerateAssemblyInfo", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{fixture: "MissingGenerateAssemblyInfo", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{fixture: "MissingProperties", expectedCount: 2, expectedScore: testsupport.IntPtr(60)},
	}
	check := checks.NewImplicitAssemblyInfo()
	for _, testCase := range testCases {
		t.Run(testCase.fixture, func(t *testing.T) {
			path := filepath.Join(t.TempDir(), testCase.fixture+".csproj")
			testsupport.WriteFile(t, path, testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "ImplicitAssemblyInfo", testCase.fixture+".csproj.xml"))))
			deductions := check.Run(path)
			testsupport.AssertFinalScore(t, deductions, testCase.expectedCount, testCase.expectedScore)
			if testCase.fixture == "MissingProperties" {
				joined := fmt.Sprint(deductions)
				if !strings.Contains(joined, "UserSecretsId") || !strings.Contains(joined, "Product") {
					t.Fatalf("expected missing property names in deductions, got %s", joined)
				}
			}
		})
	}
}
