package checks

import (
	"os"
	"path/filepath"
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type ProperDockerfile struct {
	baseCheck
}

func NewProperDockerfile() *ProperDockerfile {
	return &ProperDockerfile{baseCheck: newBaseCheck("ProperDockerfile")}
}

func (c *ProperDockerfile) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	dockerfile := filepath.Join(filepath.Dir(absolutePathToProjectFile), "Dockerfile")
	content, err := os.ReadFile(dockerfile)
	if err != nil {
		return []scorecard.Deduction{scorecard.NewDisqualification("No Dockerfile found at %v", dockerfile)}
	}
	dockerfileContent := string(content)
	if !strings.Contains(dockerfileContent, "dotnet build") && !strings.Contains(dockerfileContent, "dotnet publish") {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Dockerfile at %v does not contain 'dotnet build' or 'dotnet publish'", dockerfile)}
	}
	if !strings.Contains(dockerfileContent, "dotnet sonarscanner") {
		return []scorecard.Deduction{scorecard.NewDeduction(50, "Dockerfile at %v doesn't contain 'dotnet sonarscanner'", dockerfile)}
	}
	if strings.Contains(dockerfileContent, "--version") {
		return []scorecard.Deduction{scorecard.NewDeduction(10, "Dockerfile at %v contains pinned version that won't be caught by Renovate", dockerfile)}
	}
	return nil
}

var _ Check = (*ProperDockerfile)(nil)
