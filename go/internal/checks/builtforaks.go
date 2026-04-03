package checks

import (
	"os"
	"path/filepath"
	"sort"
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type BuiltForAKS struct {
	baseCheck
}

func NewBuiltForAKS() *BuiltForAKS {
	return &BuiltForAKS{baseCheck: newBaseCheck("BuiltForAKS")}
}

func (c *BuiltForAKS) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := loadProjectXML(absolutePathToProjectFile)
	usedSDK := project.rootAttribute("Sdk")
	relativePathToProject := filepath.ToSlash(absolutePathToProjectFile)
	if usedSDK == "" {
		return []scorecard.Deduction{scorecard.NewDisqualification("No Sdk attribute found in %v", relativePathToProject)}
	}
	if usedSDK != "Microsoft.NET.Sdk.Web" {
		return []scorecard.Deduction{scorecard.NewDisqualification("Only projects using 'Microsoft.NET.Sdk.Web' are considered deployable")}
	}

	absolutePathToProjectDirectory := filepath.Dir(absolutePathToProjectFile)
	entries, _ := filepath.Glob(filepath.Join(absolutePathToProjectDirectory, "*.yml"))
	sort.Strings(entries)
	if len(entries) == 0 {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No .yml file found inside %v", absolutePathToProjectDirectory)}
	}
	if len(entries) > 1 {
		deductions := make([]scorecard.Deduction, 0, len(entries))
		for _, entry := range entries {
			deductions = append(deductions, scorecard.NewDeduction(5, "More than one pipeline file: %v", entry))
		}
		return deductions
	}
	firstPath := entries[0]
	if strings.Contains(firstPath, "build-") || strings.Contains(firstPath, "azure-") {
		return nil
	}
	if strings.Contains(firstPath, "onprem-") {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Service pipeline file doesn't start with 'azure-'; actual: %v", firstPath)}
	}
	return []scorecard.Deduction{scorecard.NewDeduction(5, ".yml needs to start with either 'azure-' for services or 'build-' for libraries; actual: '%v'", firstPath)}
}

var _ Check = (*BuiltForAKS)(nil)

var _ = os.PathSeparator
