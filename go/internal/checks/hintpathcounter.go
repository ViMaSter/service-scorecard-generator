package checks

import (
	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type HintPathCounter struct {
	checkruntime.BaseCheck
}

func NewHintPathCounter() *HintPathCounter {
	return &HintPathCounter{BaseCheck: checkruntime.NewBaseCheck("HintPathCounter")}
}

func (c *HintPathCounter) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := checkruntime.LoadProjectXML(absolutePathToProjectFile)
	if project.DecodeErr() != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Couldn't parse %v: %v", checkruntime.RelPath(absolutePathToProjectFile), project.DecodeErr())}
	}
	hintPaths := project.AllElements("HintPath")
	deductions := make([]scorecard.Deduction, 0, len(hintPaths))
	for _, hintPath := range hintPaths {
		deductions = append(deductions, scorecard.NewDeduction(10, "HintPath: %v", hintPath))
	}
	return deductions
}

var _ checkruntime.Check = (*HintPathCounter)(nil)
