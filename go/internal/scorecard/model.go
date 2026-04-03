package scorecard

import (
	"bytes"
	"encoding/json"
	"fmt"
	"math"
	"regexp"
	"strings"
)

const (
	GoldWeight   = 10
	SilverWeight = 5
	BronzeWeight = 1
)

var messageTemplatePattern = regexp.MustCompile(`\{[^}]+\}`)

var GroupOrder = []string{"Gold", "Silver", "Bronze"}

func marshalNoHTMLEscape(v any) ([]byte, error) {
	var b bytes.Buffer
	enc := json.NewEncoder(&b)
	enc.SetEscapeHTML(false)
	if err := enc.Encode(v); err != nil {
		return nil, err
	}
	out := bytes.TrimSuffix(b.Bytes(), []byte("\n"))
	return out, nil
}

type Deduction struct {
	Justification      string `json:"Justification"`
	Score              *int   `json:"Score"`
	IsDisqualification bool   `json:"IsDisqualification"`
}

func NewDeduction(score int, justificationTemplate string, values ...any) Deduction {
	formatted := formatMessageTemplate(justificationTemplate, values...)
	return Deduction{Justification: formatted, Score: &score}
}

func NewDisqualification(justificationTemplate string, values ...any) Deduction {
	formatted := formatMessageTemplate(justificationTemplate, values...)
	return Deduction{Justification: formatted, IsDisqualification: true}
}

func formatMessageTemplate(template string, values ...any) string {
	format := messageTemplatePattern.ReplaceAllString(template, "%v")
	return fmt.Sprintf(format, values...)
}

func (d Deduction) String() string {
	if d.IsDisqualification {
		return fmt.Sprintf("disqualified: %s", d.Justification)
	}
	return fmt.Sprintf("-%d points: %s", *d.Score, d.Justification)
}

func CalculateFinalScore(deductions []Deduction) *int {
	for _, deduction := range deductions {
		if deduction.IsDisqualification {
			return nil
		}
	}
	finalScore := 100
	for _, deduction := range deductions {
		if deduction.Score != nil {
			finalScore -= *deduction.Score
		}
	}
	if finalScore < 0 {
		finalScore = 0
	}
	return &finalScore
}

type CheckInfo struct {
	Name            string `json:"Name"`
	InfoPageContent string `json:"InfoPageContent"`
}

type Group struct {
	Name   string
	Checks []CheckInfo
}

type CheckResult struct {
	Name       string
	Deductions []Deduction
}

type ServiceScorecard struct {
	Checks  []CheckResult
	Average int
}

func (s ServiceScorecard) DeductionsByCheck(checkName string) ([]Deduction, bool) {
	for _, check := range s.Checks {
		if check.Name == checkName {
			return check.Deductions, true
		}
	}
	return nil, false
}

type ServiceScore struct {
	Path  string
	Score ServiceScorecard
}

type RunInfo struct {
	Groups   []Group
	Services []ServiceScore
}

func (r RunInfo) GroupByName(name string) Group {
	for _, group := range r.Groups {
		if group.Name == name {
			return group
		}
	}
	return Group{Name: name}
}

func (r RunInfo) ToJSON() (string, error) {
	var buffer bytes.Buffer
	buffer.WriteString(`{"Checks":{`)
	for groupIndex, groupName := range GroupOrder {
		if groupIndex > 0 {
			buffer.WriteByte(',')
		}
		group := r.GroupByName(groupName)
		nameJSON, err := marshalNoHTMLEscape(groupName)
		if err != nil {
			return "", err
		}
		buffer.Write(nameJSON)
		buffer.WriteByte(':')
		buffer.WriteByte('[')
		for checkIndex, check := range group.Checks {
			if checkIndex > 0 {
				buffer.WriteByte(',')
			}
			checkJSON, err := marshalNoHTMLEscape(check)
			if err != nil {
				return "", err
			}
			buffer.Write(checkJSON)
		}
		buffer.WriteByte(']')
	}
	buffer.WriteString(`},"ServiceScores":{`)
	for serviceIndex, service := range r.Services {
		if serviceIndex > 0 {
			buffer.WriteByte(',')
		}
		serviceNameJSON, err := marshalNoHTMLEscape(service.Path)
		if err != nil {
			return "", err
		}
		buffer.Write(serviceNameJSON)
		buffer.WriteString(`:{"DeductionsByCheck":{`)
		for checkIndex, check := range service.Score.Checks {
			if checkIndex > 0 {
				buffer.WriteByte(',')
			}
			checkNameJSON, err := marshalNoHTMLEscape(check.Name)
			if err != nil {
				return "", err
			}
			buffer.Write(checkNameJSON)
			buffer.WriteByte(':')
			buffer.WriteByte('[')
			for deductionIndex, deduction := range check.Deductions {
				if deductionIndex > 0 {
					buffer.WriteByte(',')
				}
				buffer.WriteString(`{"Justification":`)
				justificationJSON, err := marshalNoHTMLEscape(deduction.Justification)
				if err != nil {
					return "", err
				}
				buffer.Write(justificationJSON)
				buffer.WriteString(`,"Score":`)
				if deduction.Score == nil {
					buffer.WriteString("null")
				} else {
					buffer.WriteString(fmt.Sprintf("%d", *deduction.Score))
				}
				buffer.WriteString(`,"IsDisqualification":`)
				if deduction.IsDisqualification {
					buffer.WriteString("true")
				} else {
					buffer.WriteString("false")
				}
				buffer.WriteByte('}')
			}
			buffer.WriteByte(']')
		}
		buffer.WriteString(`},"Average":`)
		buffer.WriteString(fmt.Sprintf("%d", service.Score.Average))
		buffer.WriteByte('}')
	}
	buffer.WriteString(`}}`)
	return buffer.String(), nil
}

type HistoricRunInfo struct {
	Checks        map[string][]CheckInfo              `json:"Checks"`
	ServiceScores map[string]HistoricServiceScorecard `json:"ServiceScores"`
}

type HistoricServiceScorecard struct {
	DeductionsByCheck map[string][]Deduction `json:"DeductionsByCheck"`
	Average           int                    `json:"Average"`
}

func ParseHistoricRunInfo(content string) (*HistoricRunInfo, error) {
	var runInfo HistoricRunInfo
	if err := json.Unmarshal([]byte(content), &runInfo); err != nil {
		return nil, err
	}
	return &runInfo, nil
}

func CalculateAverage(goldChecks []CheckResult, silverChecks []CheckResult, bronzeChecks []CheckResult) int {
	totalScore := 0
	totalChecks := 0

	for _, check := range goldChecks {
		if score := CalculateFinalScore(check.Deductions); score != nil {
			totalScore += *score * GoldWeight
			totalChecks += GoldWeight
		}
	}
	for _, check := range silverChecks {
		if score := CalculateFinalScore(check.Deductions); score != nil {
			totalScore += *score * SilverWeight
			totalChecks += SilverWeight
		}
	}
	for _, check := range bronzeChecks {
		if score := CalculateFinalScore(check.Deductions); score != nil {
			totalScore += *score * BronzeWeight
			totalChecks += BronzeWeight
		}
	}

	if totalChecks == 0 {
		return 0
	}
	return int(math.RoundToEven(float64(totalScore) / float64(totalChecks)))
}

func NormalizeDatePlaceholder(content string, now string) string {
	return strings.ReplaceAll(strings.ReplaceAll(content, "YYYY-MM-DD", now), "\r\n", "\n")
}
