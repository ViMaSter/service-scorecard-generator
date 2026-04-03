package checks

import (
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type NullableSetup struct {
	baseCheck
}

func NewNullableSetup() *NullableSetup {
	return &NullableSetup{baseCheck: newBaseCheck("NullableSetup")}
}

func (c *NullableSetup) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := loadProjectXML(absolutePathToProjectFile)
	nullable := project.firstElement("Nullable")
	if nullable == "" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No <Nullable> element found in %v", absolutePathToProjectFile)}
	}
	if strings.ToLower(nullable) != "enable" {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Expected: <Nullable> should contain '%v'. Actual: '%v'", "enable", nullable)}
	}
	return nil
}

var _ Check = (*NullableSetup)(nil)
