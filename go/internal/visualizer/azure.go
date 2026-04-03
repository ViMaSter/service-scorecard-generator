package visualizer

import (
	"fmt"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type AzureWikiTableVisualizer struct {
	outputPath string
	now        func() time.Time
}

func NewAzureWikiTableVisualizer(outputPath string, now func() time.Time) *AzureWikiTableVisualizer {
	if now == nil {
		now = time.Now
	}
	return &AzureWikiTableVisualizer{outputPath: outputPath, now: now}
}

func (v *AzureWikiTableVisualizer) Visualize(runInfo scorecard.RunInfo) error {
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
	alternateColorIndex := 1
	toBackgroundColor := func() string {
		alternateColorIndex++
		if alternateColorIndex%2 == 0 {
			return "background-color: rgba(0, 0, 0, 0.05);"
		}
		return ""
	}
	styleForElement := func(colorIndex int) string {
		style := ""
		if colorIndex <= 3 {
			style += "background-color: rgba(var(--palette-neutral-2),1);"
		}
		return fmt.Sprintf(`style="%s"`, style)
	}
	toElement := func(element string, columns []tableContent) string {
		parts := make([]string, 0, len(columns))
		background := toBackgroundColor()
		for _, column := range columns {
			parts = append(parts, fmt.Sprintf(`<%s title="%s" %s colspan="%d">%s</%s>`, element, column.Title, styleForElement(alternateColorIndex), column.Colspan, column.Content, element))
		}
		return fmt.Sprintf(`<tr style="%s">%s</tr>`, background, strings.Join(parts, ""))
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
		columns := []tableContent{{Content: fmt.Sprintf("<span>%s%s</span>", strings.TrimSuffix(filepathBase(service.Path), ".csproj"), questionMark), Title: service.Path, Colspan: 1}}
		for _, check := range service.Score.Checks {
			columns = append(columns, formatPlainScore(check.Deductions, getHistoricDeductions(historicRunInfo, service.Path, check.Name), `<sub><span title="compared to 7 days ago" style="color: rgba(var(--palette-accent2),1)"> ↑+%d%%</span></sub>`, `<sub><span title="compared to 7 days ago" style="color: rgba(var(--palette-accent1),1)"> ↓%d%%</span></sub>`, func(score *int) string {
				if score == nil {
					return "color:var(--status-info-foreground)"
				}
				switch {
				case *score >= 90:
					return "color:rgba(var(--palette-accent2),1)"
				case *score >= 80:
					return "color:rgba(var(--palette-accent3),1)"
				case *score >= 70:
					return "color:var(--status-warning-icon-foreground)"
				default:
					return "color:rgba(var(--palette-accent1),1)"
				}
			}))
		}
		average := service.Score.Average
		columns = append(columns, tableContent{Content: fmt.Sprintf(`<span title style="%s">%d</span>`, func() string {
			switch {
			case average >= 90:
				return "color:rgba(var(--palette-accent2),1)"
			case average >= 80:
				return "color:rgba(var(--palette-accent3),1)"
			case average >= 70:
				return "color:var(--status-warning-icon-foreground)"
			default:
				return "color:rgba(var(--palette-accent1),1)"
			}
		}(), average), Colspan: 1})
		rows = append(rows, toElement("td", columns))
	}
	content := fmt.Sprintf("# Service Scorecard for %s\n\nInformation on how to reach 100 points for each check can be found in the child pages:\n[[_TOSP_]]\n\n# Hover over cells for details\n\n<table id=\"service-scorecard\">\n%s</table>\n\n<!-- %s -->", v.now().Format("2006-01-02"), strings.Join(rows, "\n"), runInfoJSON)
	return writeGeneratedOutput(v.outputPath, fileName+".md", content, true)
}
