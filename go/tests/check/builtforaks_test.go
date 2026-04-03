package check_test

import (
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestBuiltForAKSScenarios(t *testing.T) {
	testCases := []struct {
		name          string
		projectXML    string
		pipelineFiles []string
		expectedCount int
		expectedScore *int
	}{
		{name: "azure pipeline", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, pipelineFiles: []string{"azure-pipelines.yml"}, expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "build pipeline", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, pipelineFiles: []string{"build-pipelines.yml"}, expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
		{name: "onprem pipeline", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, pipelineFiles: []string{"onprem-pipelines.yml"}, expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "five pipeline files", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, pipelineFiles: []string{"azure-pipelines0.yml", "azure-pipelines1.yml", "azure-pipelines2.yml", "azure-pipelines3.yml", "azure-pipelines4.yml"}, expectedCount: 5, expectedScore: testsupport.IntPtr(75)},
		{name: "no pipeline files", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, expectedCount: 1, expectedScore: testsupport.IntPtr(0)},
		{name: "missing sdk", projectXML: `<Project></Project>`, expectedCount: 1, expectedScore: nil},
		{name: "non web sdk", projectXML: `<Project Sdk="Microsoft.NET.Sdk"></Project>`, expectedCount: 1, expectedScore: nil},
		{name: "unknown pipeline", projectXML: `<Project Sdk="Microsoft.NET.Sdk.Web"></Project>`, pipelineFiles: []string{"unknown-pipelines.yml"}, expectedCount: 1, expectedScore: testsupport.IntPtr(95)},
	}
	for _, testCase := range testCases {
		t.Run(testCase.name, func(t *testing.T) {
			tempDirectory := t.TempDir()
			projectPath := filepath.Join(tempDirectory, "test.csproj")
			testsupport.WriteFile(t, projectPath, testCase.projectXML)
			for _, pipelineFile := range testCase.pipelineFiles {
				testsupport.WriteFile(t, filepath.Join(tempDirectory, pipelineFile), "")
			}
			check := checks.NewBuiltForAKS()
			testsupport.AssertFinalScore(t, check.Run(projectPath), testCase.expectedCount, testCase.expectedScore)
		})
	}
}
