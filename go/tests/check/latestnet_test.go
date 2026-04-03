package check_test

import (
	"fmt"
	"net/http"
	"path/filepath"
	"strings"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestLatestNETFixtures(t *testing.T) {
	check := checks.NewLatestNET(http.DefaultClient)
	testCases := []struct {
		fixture       string
		expectedCount int
		expectedScore *int
		beforeRun     func(t *testing.T, path string)
	}{
		{fixture: "NET6", expectedCount: 1, expectedScore: testsupport.IntPtr(int((6.0/float64(check.NewestMajor))*100 + 0.5))},
		{fixture: "NETCore31", expectedCount: 1, expectedScore: testsupport.IntPtr(int((3.0/float64(check.NewestMajor))*100 + 0.5))},
		{fixture: "NETFramework471", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{fixture: "NoTargetFramework", expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{fixture: "NotYetReleased", expectedCount: 1, expectedScore: testsupport.IntPtr(95)},
		{fixture: "NewestMajor", expectedCount: 0, expectedScore: testsupport.IntPtr(100), beforeRun: func(t *testing.T, path string) {
			content := testsupport.ReadFile(t, path)
			testsupport.WriteFile(t, path, strings.ReplaceAll(content, "__NEWEST_MAJOR__", fmt.Sprintf("%d.0", check.NewestMajor)))
		}},
	}
	for _, testCase := range testCases {
		t.Run(testCase.fixture, func(t *testing.T) {
			tempDirectory := t.TempDir()
			path := filepath.Join(tempDirectory, testCase.fixture+".csproj")
			testsupport.WriteFile(t, path, testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "LatestNET", testCase.fixture+".csproj.xml"))))
			if testCase.beforeRun != nil {
				testCase.beforeRun(t, path)
			}
			testsupport.AssertFinalScore(t, check.Run(path), testCase.expectedCount, testCase.expectedScore)
		})
	}
}
