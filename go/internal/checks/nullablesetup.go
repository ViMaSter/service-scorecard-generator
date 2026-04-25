package checks

import (
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type NullableSetup struct {
	checkruntime.BaseCheck
}

func NewNullableSetup() *NullableSetup {
	return &NullableSetup{BaseCheck: checkruntime.NewBaseCheck("NullableSetup")}
}

func (c *NullableSetup) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := checkruntime.LoadProjectXML(absolutePathToProjectFile)
	nullable := project.FirstElement("Nullable")
	if nullable == "" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No <Nullable> element found in %v", checkruntime.RelPath(absolutePathToProjectFile))}
	}
	if strings.ToLower(nullable) != "enable" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Expected: <Nullable> should contain '%v'. Actual: '%v'", "enable", nullable)}
	}
	return nil
}

var _ checkruntime.Check = (*NullableSetup)(nil)
