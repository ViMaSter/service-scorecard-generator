package check_test

import (
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestHintPathCounterFixtures(t *testing.T) {
	testCases := []struct {
		fixture       string
		expectedCount int
		expectedScore *int
	}{
		{fixture: "EmptyProjectFile", expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{fixture: "HasNoHintPath", expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{fixture: "HasTwoHintPaths", expectedCount: 2, expectedScore: testsupport.IntPtr(80)},
		{fixture: "HasElevenHintPaths", expectedCount: 11, expectedScore: testsupport.IntPtr(0)},
	}
	check := checks.NewHintPathCounter()
	for _, testCase := range testCases {
		t.Run(testCase.fixture, func(t *testing.T) {
			path := filepath.Join(t.TempDir(), testCase.fixture+".csproj")
			testsupport.WriteFile(t, path, testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "HintPathCounter", testCase.fixture+".csproj.xml"))))
			testsupport.AssertFinalScore(t, check.Run(path), testCase.expectedCount, testCase.expectedScore)
		})
	}
}
