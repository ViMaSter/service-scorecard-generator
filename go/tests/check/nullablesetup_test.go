package check_test

import (
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestNullableSetupFixtures(t *testing.T) {
	testCases := []struct {
		fixture       string
		expectedCount int
		expectedScore *int
	}{
		{fixture: "NullableEnabled", expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{fixture: "NullableDisabled", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{fixture: "NoNullableElement", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
	}
	check := checks.NewNullableSetup()
	for _, testCase := range testCases {
		t.Run(testCase.fixture, func(t *testing.T) {
			path := filepath.Join(t.TempDir(), testCase.fixture+".csproj")
			testsupport.WriteFile(t, path, testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "NullableSetup", testCase.fixture+".csproj.xml"))))
			testsupport.AssertFinalScore(t, check.Run(path), testCase.expectedCount, testCase.expectedScore)
		})
	}
}
