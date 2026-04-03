package checks

import "github.com/vimaster/service-scorecard-generator/go/internal/scorecard"

type HintPathCounter struct {
	baseCheck
}

func NewHintPathCounter() *HintPathCounter {
	return &HintPathCounter{baseCheck: newBaseCheck("HintPathCounter")}
}

func (c *HintPathCounter) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	project := loadProjectXML(absolutePathToProjectFile)
	if project.decodeErr != nil {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "Couldn't parse %v: %v", absolutePathToProjectFile, project.decodeErr)}
	}
	hintPaths := project.allElements("HintPath")
	deductions := make([]scorecard.Deduction, 0, len(hintPaths))
	for _, hintPath := range hintPaths {
		deductions = append(deductions, scorecard.NewDeduction(10, "HintPath: %v", hintPath))
	}
	return deductions
}

var _ Check = (*HintPathCounter)(nil)
