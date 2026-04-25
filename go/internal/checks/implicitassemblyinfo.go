package checks

import (
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type ImplicitAssemblyInfo struct {
	checkruntime.BaseCheck
}

func NewImplicitAssemblyInfo() *ImplicitAssemblyInfo {
	return &ImplicitAssemblyInfo{BaseCheck: checkruntime.NewBaseCheck("ImplicitAssemblyInfo")}
}

func (c *ImplicitAssemblyInfo) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := checkruntime.LoadProjectXML(absolutePathToProjectFile)
	requiredProperties := []string{"Company", "Copyright", "Description", "FileVersion", "InformalVersion", "Product", "UserSecretsId"}
	deductions := make([]scorecard.Deduction, 0)
	for _, propertyName := range requiredProperties {
		if project.FirstElement(propertyName) == "" {
			deductions = append(deductions, scorecard.NewDeduction(20, "No <%v> element found in %v", propertyName, checkruntime.RelPath(absolutePathToProjectFile)))
		}
	}
	generateAssemblyInfo := project.FirstElement("GenerateAssemblyInfo")
	if generateAssemblyInfo == "" {
		return append(deductions, scorecard.NewDeduction(100, "No <GenerateAssemblyInfo> element found in %v", checkruntime.RelPath(absolutePathToProjectFile)))
	}
	if strings.ToLower(generateAssemblyInfo) != "true" {
		return append(deductions, scorecard.NewDeduction(100, "Expected: <GenerateAssemblyInfo> should contain '%v'. Actual: '%v'", "true", generateAssemblyInfo))
	}
	return deductions
}

var _ checkruntime.Check = (*ImplicitAssemblyInfo)(nil)
