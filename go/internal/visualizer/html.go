package visualizer

import (
	"bytes"
	"fmt"
	"regexp"
	"strings"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/resources"
	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/yuin/goldmark"
)

type HTMLVisualizer struct {
	outputPath string
	now        func() time.Time
}

func NewHTMLVisualizer(outputPath string, now func() time.Time) *HTMLVisualizer {
	if now == nil {
		now = time.Now
	}
	return &HTMLVisualizer{outputPath: outputPath, now: now}
}

func (v *HTMLVisualizer) Visualize(runInfo scorecard.RunInfo) error {
	historicRunInfo, _ := getHistoricRunInfo(v.outputPath, v.now(), "index.html")
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
			columns = append(columns, formatPlainScore(check.Deductions, getHistoricDeductions(historicRunInfo, service.Path, check.Name), `<sub><span title="compared to 7 days ago" style="color: rgba(var(--palette-accent2),1)"> ↑+%d%%</span></sub>`, `<sub><span title="compared to 7 days ago" style="color: rgba(var(--palette-accent1),1)"> ↓%d%%</span></sub>`, func(score *int) string {
				if score == nil {
					return "color:rgba(var(--status-info-foreground),1)"
				}
				switch {
				case *score >= 90:
					return "color:rgba(var(--palette-accent2),1)"
				case *score >= 80:
					return "color:rgba(var(--palette-accent3),1)"
				case *score >= 70:
					return "color:rgba(var(--status-warning-icon-foreground),1)"
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
				return "color:rgba(var(--status-warning-icon-foreground),1)"
			default:
				return "color:rgba(var(--palette-accent1),1)"
			}
		}(), average), Colspan: 1})
		rows = append(rows, toElement("td", columns))
	}
	lists := buildCheckHTML(runInfo)
	content := resources.HTMLVisualizerTemplate
	replacements := map[string]string{
		"@headline":      fmt.Sprintf("Service Scorecard for %s", v.now().Format("2006-01-02")),
		"@table":         fmt.Sprintf("<table id=\"service-scorecard\">\n%s</table>", strings.Join(rows, "\n")),
		"@data":          runInfoJSON,
		"@lists":         lists,
		"@lastUpdatedAt": v.now().Format(time.RFC3339),
	}
	for key, value := range replacements {
		content = strings.ReplaceAll(content, key, value)
	}
	return writeGeneratedOutput(v.outputPath, "index.html", content, false)
}

func buildCheckHTML(runInfo scorecard.RunInfo) string {
	items := make([]string, 0)
	markdown := goldmark.New()
	for _, groupName := range scorecard.GroupOrder {
		for _, check := range runInfo.GroupByName(groupName).Checks {
			var rendered bytes.Buffer
			_ = markdown.Convert([]byte(removeFirstHeading(check.InfoPageContent)), &rendered)
			items = append(items, fmt.Sprintf("<li><details><summary>%s</summary><div>%s</div></details></li>", check.Name, strings.TrimSpace(rendered.String())))
		}
	}
	return strings.Join(items, "")
}

func HTMLRemoveDates(content string, now time.Time) string {
	pattern := regexp.MustCompile(`<noscript>.*</noscript>`)
	content = pattern.ReplaceAllString(content, "")
	return scorecard.NormalizeDatePlaceholder(content, now.Format("2006-01-02"))
}
