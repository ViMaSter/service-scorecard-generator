package checks

import (
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type ImplicitAssemblyInfo struct {
	baseCheck
}

func NewImplicitAssemblyInfo() *ImplicitAssemblyInfo {
	return &ImplicitAssemblyInfo{baseCheck: newBaseCheck("ImplicitAssemblyInfo")}
}

func (c *ImplicitAssemblyInfo) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := loadProjectXML(absolutePathToProjectFile)
	requiredProperties := []string{"Company", "Copyright", "Description", "FileVersion", "InformalVersion", "Product", "UserSecretsId"}
	deductions := make([]scorecard.Deduction, 0)
	for _, propertyName := range requiredProperties {
		if project.firstElement(propertyName) == "" {
			deductions = append(deductions, scorecard.NewDeduction(20, "No <%v> element found in %v", propertyName, absolutePathToProjectFile))
		}
	}
	generateAssemblyInfo := project.firstElement("GenerateAssemblyInfo")
	if generateAssemblyInfo == "" {
		return append(deductions, scorecard.NewDeduction(100, "No <GenerateAssemblyInfo> element found in %v", absolutePathToProjectFile))
	}
	if strings.ToLower(generateAssemblyInfo) != "true" {
		return append(deductions, scorecard.NewDeduction(100, "Expected: <GenerateAssemblyInfo> should contain '%v'. Actual: '%v'", "true", generateAssemblyInfo))
	}
	return deductions
}

var _ Check = (*ImplicitAssemblyInfo)(nil)
