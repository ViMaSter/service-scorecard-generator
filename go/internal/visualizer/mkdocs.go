package visualizer

import (
	"fmt"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type MkDocsMarkdownVisualizer struct {
	outputPath string
	now        func() time.Time
}

func NewMkDocsMarkdownVisualizer(outputPath string, now func() time.Time) *MkDocsMarkdownVisualizer {
	if now == nil {
		now = time.Now
	}
	return &MkDocsMarkdownVisualizer{outputPath: outputPath, now: now}
}

func (v *MkDocsMarkdownVisualizer) Visualize(runInfo scorecard.RunInfo) error {
	if err := ensureScorecardDirectory(v.outputPath); err != nil {
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
		columns := []tableContent{{Content: fmt.Sprintf("<span>%s%s</span>", strings.TrimSuffix(filepathBase(service.Path), ".csproj"), questionMark), Title: service.Path, Colspan: 1}}
		for _, check := range service.Score.Checks {
			columns = append(columns, formatMkDocsScore(check.Deductions, getHistoricDeductions(historicRunInfo, service.Path, check.Name)))
		}
		average := service.Score.Average
		columns = append(columns, tableContent{Content: fmt.Sprintf(`<span title style="%s">%d</span>`, styleOfNumber(&average), average), Colspan: 1})
		rows = append(rows, toElement("td", columns))
	}
	table := fmt.Sprintf("<table id=\"service-scorecard\">\n%s</table>", strings.Join(rows, "\n"))
	checkGuides := make([]string, 0)
	for _, groupName := range scorecard.GroupOrder {
		for _, check := range runInfo.GroupByName(groupName).Checks {
			checkGuides = append(checkGuides, fmt.Sprintf("??? info \"%s\"\n%s", check.Name, indentBlock(removeFirstHeading(check.InfoPageContent))))
		}
	}
	content := fmt.Sprintf("# Service Scorecard for %s\n\n!!! info \"Usage\"\n    Information on how to reach 100 points for each check can be found below the table.\n    Hover over cells for detailed deductions.\n\n## Service Overview\n\n%s\n\n## Check Details\n\n%s\n\n<!-- %s -->", v.now().Format("2006-01-02"), table, strings.Join(checkGuides, "\n\n"), runInfoJSON)
	return writeGeneratedOutput(v.outputPath, fileName+".md", content, true)
}

func filepathBase(path string) string {
	parts := strings.Split(strings.ReplaceAll(path, "\\", "/"), "/")
	return parts[len(parts)-1]
}
