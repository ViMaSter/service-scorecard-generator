package visualizer

import (
	"fmt"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type GitLabMarkdownVisualizer struct {
	outputPath string
	now        func() time.Time
}

func NewGitLabMarkdownVisualizer(outputPath string, now func() time.Time) *GitLabMarkdownVisualizer {
	if now == nil {
		now = time.Now
	}
	return &GitLabMarkdownVisualizer{outputPath: outputPath, now: now}
}

func (v *GitLabMarkdownVisualizer) Visualize(runInfo scorecard.RunInfo) error {
	if err := ensureScorecardDirectory(v.outputPath); err != nil {
		return err
	}
	if err := generateCheckPages(v.outputPath, runInfo); err != nil {
		return err
	}
	historicRunInfo, _ := getHistoricRunInfo(v.outputPath, v.now(), fileName+".md")
	runInfoJSON, err := runInfo.ToJSON()
	if err != nil {
		return err
	}
	toElement := func(element string, columns []tableContent) string {
		parts := make([]string, 0, len(columns))
		for _, column := range columns {
			parts = append(parts, fmt.Sprintf(`<%s title="%s" colspan="%d">%s</%s>`, element, column.Title, column.Colspan, column.Content, element))
		}
		return "<tr>" + strings.Join(parts, "") + "</tr>"
	}
	groupHeader := []tableContent{{Content: "   ", Colspan: 1}}
	columnHeader := []tableContent{{Content: "ServiceName", Colspan: 1}}
	for _, groupName := range scorecard.GroupOrder {
		group := runInfo.GroupByName(groupName)
		if len(group.Checks) == 0 {
			continue
		}
		groupHeader = append(groupHeader, tableContent{Content: group.Name, Colspan: len(group.Checks)})
		for _, check := range group.Checks {
			columnHeader = append(columnHeader, tableContent{Content: check.Name, Colspan: 1})
		}
	}
	groupHeader = append(groupHeader, tableContent{Content: "   ", Colspan: 1})
	columnHeader = append(columnHeader, tableContent{Content: "Average", Colspan: 1})
	rows := []string{toElement("th", groupHeader), toElement("th", columnHeader)}
	for _, service := range runInfo.Services {
		columns := []tableContent{{Content: fmt.Sprintf(`<span title="%s">%s%s</span>`, escapeTitle(service.Path), strings.TrimSuffix(filepathBase(service.Path), ".csproj"), questionMark), Colspan: 1}}
		for _, check := range service.Score.Checks {
			formatted := formatGitLabScore(check.Deductions, getHistoricDeductions(historicRunInfo, service.Path, check.Name))
			columns = append(columns, tableContent{Content: formatted.Content, Colspan: 1})
		}
		average := service.Score.Average
		columns = append(columns, tableContent{Content: toLatexScore(fmt.Sprintf("%d", average), &average), Colspan: 1})
		rows = append(rows, toElement("td", columns))
	}
	content := fmt.Sprintf("# Service Scorecard for %s\n\nInformation on how to reach 100 points for each check can be found in the child pages:\n[[_TOSP_]]\n\n<table id=\"service-scorecard\">\n%s</table>\n\n<!-- %s -->", v.now().Format("2006-01-02"), strings.Join(rows, "\n"), runInfoJSON)
	return writeGeneratedOutput(v.outputPath, fileName+".md", content, true)
}

func formatGitLabScore(checkValue []scorecard.Deduction, sevenDaysAgo []scorecard.Deduction) tableContent {
	finalScore := scorecard.CalculateFinalScore(checkValue)
	var finalScoreSevenDaysAgo *int
	if sevenDaysAgo != nil {
		finalScoreSevenDaysAgo = scorecard.CalculateFinalScore(sevenDaysAgo)
	}
	deltaString := formatDelta(finalScore, finalScoreSevenDaysAgo, ` <sub><span title="compared to 7 days ago">↑+%d%%</span></sub>`, ` <sub><span title="compared to 7 days ago">↓%d%%</span></sub>`)
	justifications := make([]string, 0, len(checkValue))
	for _, deduction := range checkValue {
		justifications = append(justifications, deduction.String())
	}
	display := "n/a"
	if finalScore != nil {
		display = fmt.Sprintf("%d", *finalScore)
	}
	return tableContent{Content: fmt.Sprintf(`<span title="%s">%s%s</span>`, escapeTitle(strings.Join(justifications, "&#10;")), toLatexScore(display, finalScore), deltaString)}
}
