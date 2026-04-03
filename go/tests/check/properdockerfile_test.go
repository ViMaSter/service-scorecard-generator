package check_test

import (
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestProperDockerfileFixtures(t *testing.T) {
	testCases := []struct {
		directory      string
		fixture        string
		expectedCount  int
		expectedScore  *int
		disqualified   bool
		withDockerfile bool
	}{
		{directory: "Result100", fixture: "DotnetBuildAndSonarscanner", expectedCount: 0, expectedScore: testsupport.IntPtr(100), withDockerfile: true},
		{directory: "Result100", fixture: "DotnetPublishAndSonarscanner", expectedCount: 0, expectedScore: testsupport.IntPtr(100), withDockerfile: true},
		{directory: "Result50", fixture: "DotnetBuildOnly", expectedCount: 1, expectedScore: testsupport.IntPtr(50), withDockerfile: true},
		{directory: "Result90", fixture: "PinnedVersion", expectedCount: 1, expectedScore: testsupport.IntPtr(90), withDockerfile: true},
		{directory: "Result0", fixture: "NoBuild", expectedCount: 1, expectedScore: testsupport.IntPtr(0), withDockerfile: true},
		{directory: "ResultSkipped", fixture: "NoDockerfile", expectedCount: 1, expectedScore: nil, disqualified: true},
	}
	check := checks.NewProperDockerfile()
	for _, testCase := range testCases {
		t.Run(testCase.fixture, func(t *testing.T) {
			tempDirectory := t.TempDir()
			projectPath := filepath.Join(tempDirectory, testCase.fixture+".csproj")
			testsupport.WriteFile(t, projectPath, testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "ProperDockerfile", testCase.directory, testCase.fixture+".csproj.xml"))))
			if testCase.withDockerfile {
				testsupport.WriteFile(t, filepath.Join(tempDirectory, "Dockerfile"), testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Checks", "ProperDockerfile", testCase.directory, testCase.fixture+".Dockerfile"))))
			}
			deductions := check.Run(projectPath)
			testsupport.AssertFinalScore(t, deductions, testCase.expectedCount, testCase.expectedScore)
			if testCase.disqualified && !deductions[0].IsDisqualification {
				t.Fatalf("expected disqualification, got %+v", deductions[0])
			}
		})
	}
}
