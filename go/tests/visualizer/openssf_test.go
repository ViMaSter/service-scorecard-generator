package visualizer_test

import (
	"encoding/json"
	"path/filepath"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/visualizer"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

type openSSFRepo struct {
	Name string `json:"name"`
}

type openSSFCheck struct {
	Name    string   `json:"name"`
	Score   float64  `json:"score"`
	Reason  string   `json:"reason"`
	Details []string `json:"details"`
}

type openSSFResult struct {
	Date   string         `json:"date"`
	Repo   openSSFRepo    `json:"repo"`
	Score  float64        `json:"score"`
	Checks []openSSFCheck `json:"checks"`
}

func TestOpenSSFJSONVisualizerWritesExpectedSchema(t *testing.T) {
	now := time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC)
	outputDirectory := t.TempDir()
	v := visualizer.NewOpenSSFJSONVisualizer(outputDirectory, func() time.Time { return now })
	if err := v.Visualize(testsupport.SampleRunInfo()); err != nil {
		t.Fatal(err)
	}
	content := testsupport.ReadFile(t, filepath.Join(outputDirectory, "openssf-scorecards.json"))
	var results []openSSFResult
	if err := json.Unmarshal([]byte(content), &results); err != nil {
		t.Fatal(err)
	}
	if len(results) != 2 {
		t.Fatalf("expected 2 service results, got %d", len(results))
	}
	if results[0].Repo.Name != "service" {
		t.Fatalf("expected first service to be service, got %s", results[0].Repo.Name)
	}
	if results[0].Score != 1.0 {
		t.Fatalf("expected service score 1.0, got %v", results[0].Score)
	}
	if len(results[0].Checks) != 6 {
		t.Fatalf("expected 6 checks, got %d", len(results[0].Checks))
	}
	if results[0].Checks[1].Score != -1 {
		t.Fatalf("expected disqualified check score -1, got %v", results[0].Checks[1].Score)
	}
	if results[0].Checks[0].Reason != "1 deduction(s)" {
		t.Fatalf("unexpected reason: %s", results[0].Checks[0].Reason)
	}
	if len(results[0].Checks[0].Details) == 0 {
		t.Fatal("expected at least one detail entry")
	}
}
