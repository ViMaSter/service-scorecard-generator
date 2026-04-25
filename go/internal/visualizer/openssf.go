package visualizer

import (
	"encoding/json"
	"fmt"
	"math"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type OpenSSFJSONVisualizer struct {
	outputPath string
	now        func() time.Time
}

type openSSFRepo struct {
	Name string `json:"name"`
}

type openSSFCheckResult struct {
	Name          string   `json:"name"`
	Score         float64  `json:"score"`
	Reason        string   `json:"reason"`
	Details       []string `json:"details,omitempty"`
	Documentation string   `json:"documentation,omitempty"`
}

type openSSFResult struct {
	Date   string               `json:"date"`
	Repo   openSSFRepo          `json:"repo"`
	Score  float64              `json:"score"`
	Checks []openSSFCheckResult `json:"checks"`
}

func NewOpenSSFJSONVisualizer(outputPath string, now func() time.Time) *OpenSSFJSONVisualizer {
	if now == nil {
		now = time.Now
	}
	return &OpenSSFJSONVisualizer{outputPath: outputPath, now: now}
}

func (v *OpenSSFJSONVisualizer) Visualize(runInfo scorecard.RunInfo) error {
	checksByName := mapCheckInfoByName(runInfo)
	results := make([]openSSFResult, 0, len(runInfo.Services))
	for _, service := range runInfo.Services {
		checks := make([]openSSFCheckResult, 0, len(service.Score.Checks))
		for _, check := range service.Score.Checks {
			checks = append(checks, toOpenSSFCheck(check, checksByName[check.Name]))
		}
		results = append(results, openSSFResult{
			Date:   v.now().Format(time.RFC3339),
			Repo:   openSSFRepo{Name: service.Path},
			Score:  scoreToOpenSSF(float64(service.Score.Average)),
			Checks: checks,
		})
	}
	content, err := json.MarshalIndent(results, "", "  ")
	if err != nil {
		return err
	}
	return writeGeneratedOutput(v.outputPath, "openssf-scorecards.json", string(content), false)
}

func mapCheckInfoByName(runInfo scorecard.RunInfo) map[string]scorecard.CheckInfo {
	result := make(map[string]scorecard.CheckInfo)
	for _, groupName := range scorecard.GroupOrder {
		group := runInfo.GroupByName(groupName)
		for _, check := range group.Checks {
			result[check.Name] = check
		}
	}
	return result
}

func toOpenSSFCheck(check scorecard.CheckResult, checkInfo scorecard.CheckInfo) openSSFCheckResult {
	details := make([]string, 0, len(check.Deductions))
	for _, deduction := range check.Deductions {
		details = append(details, deduction.String())
	}
	finalScore := scorecard.CalculateFinalScore(check.Deductions)
	reason := "No deductions"
	score := 10.0
	if finalScore == nil {
		reason = "Disqualified"
		score = -1
	} else {
		score = scoreToOpenSSF(float64(*finalScore))
		if len(check.Deductions) > 0 {
			reason = fmt.Sprintf("%d deduction(s)", len(check.Deductions))
		}
	}
	return openSSFCheckResult{
		Name:          check.Name,
		Score:         score,
		Reason:        reason,
		Details:       details,
		Documentation: firstHeadingText(checkInfo.InfoPageContent),
	}
}

func firstHeadingText(content string) string {
	for _, line := range strings.Split(strings.ReplaceAll(content, "\r\n", "\n"), "\n") {
		line = strings.TrimSpace(line)
		if strings.HasPrefix(line, "#") {
			return strings.TrimSpace(strings.TrimLeft(line, "#"))
		}
	}
	return ""
}

func scoreToOpenSSF(score float64) float64 {
	return math.Round((score/10)*10) / 10
}
