package testsupport

import (
	"embed"
	"fmt"
	"io/fs"
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

//go:embed fixtures/** fixtures/Checks/PendingRenovateAzurePRs/HTTPS/_git fixtures/Checks/PendingRenovateAzurePRs/HTTPS/_git/**
var fixtures embed.FS

func ReadFile(t testing.TB, path string) string {
	t.Helper()
	content, err := os.ReadFile(path)
	if err != nil {
		t.Fatal(err)
	}
	return string(content)
}

func WriteFile(t testing.TB, path string, content string) {
	t.Helper()
	if err := os.MkdirAll(filepath.Dir(path), 0o755); err != nil {
		t.Fatal(err)
	}
	if err := os.WriteFile(path, []byte(content), 0o644); err != nil {
		t.Fatal(err)
	}
}

func Normalized(text string) string {
	return strings.ReplaceAll(text, "\r\n", "\n")
}

func AssertFinalScore(t testing.TB, deductions []scorecard.Deduction, expectedCount int, expectedFinalScore *int) {
	t.Helper()
	if len(deductions) != expectedCount {
		t.Fatalf("expected %d deductions, got %d: %+v", expectedCount, len(deductions), deductions)
	}
	actual := scorecard.CalculateFinalScore(deductions)
	if expectedFinalScore == nil && actual == nil {
		return
	}
	if expectedFinalScore == nil || actual == nil || *expectedFinalScore != *actual {
		expectedValue := "nil"
		actualValue := "nil"
		if expectedFinalScore != nil {
			expectedValue = fmt.Sprintf("%d", *expectedFinalScore)
		}
		if actual != nil {
			actualValue = fmt.Sprintf("%d", *actual)
		}
		t.Fatalf("expected final score %s, got %s", expectedValue, actualValue)
	}
}

func IntPtr(value int) *int {
	return &value
}

func ReplaceTodayPlaceholder(content string, now time.Time) string {
	return scorecard.NormalizeDatePlaceholder(content, now.Format("2006-01-02"))
}

func FixtureRead(t testing.TB, path string) string {
	t.Helper()
	content, err := fs.ReadFile(fixtures, path)
	if err != nil {
		t.Fatal(err)
	}
	return string(content)
}

func FixtureCopyTree(t testing.TB, fixtureDir string, targetDir string, mutator func(relPath string, content string) string) {
	t.Helper()
	err := fs.WalkDir(fixtures, fixtureDir, func(path string, entry fs.DirEntry, walkErr error) error {
		if walkErr != nil {
			return walkErr
		}
		relPath, err := filepath.Rel(fixtureDir, path)
		if err != nil {
			return err
		}
		if relPath == "." {
			return nil
		}
		targetPath := filepath.Join(targetDir, filepath.FromSlash(relPath))
		if entry.IsDir() {
			return os.MkdirAll(targetPath, 0o755)
		}
		contentBytes, err := fs.ReadFile(fixtures, path)
		if err != nil {
			return err
		}
		content := string(contentBytes)
		if mutator != nil {
			content = mutator(strings.ReplaceAll(relPath, "\\", "/"), content)
		}
		if err := os.MkdirAll(filepath.Dir(targetPath), 0o755); err != nil {
			return err
		}
		return os.WriteFile(targetPath, []byte(content), 0o644)
	})
	if err != nil {
		t.Fatal(err)
	}
}

func SampleRunInfo() scorecard.RunInfo {
	groups := []scorecard.Group{
		{Name: "Gold", Checks: []scorecard.CheckInfo{{Name: "Check", InfoPageContent: "PageContent"}, {Name: "DisqualifiedCheck", InfoPageContent: "Disqualified PageContent"}}},
		{Name: "Silver", Checks: []scorecard.CheckInfo{{Name: "Check2", InfoPageContent: "PageContent"}, {Name: "DisqualifiedCheck2", InfoPageContent: "Disqualified PageContent"}}},
		{Name: "Bronze", Checks: []scorecard.CheckInfo{{Name: "Check3", InfoPageContent: "PageContent"}, {Name: "DisqualifiedCheck3", InfoPageContent: "Disqualified PageContent"}}},
	}
	serviceChecks := []scorecard.CheckResult{
		{Name: "Check", Deductions: []scorecard.Deduction{scorecard.NewDeduction(10, "justification: %v", "value")}},
		{Name: "DisqualifiedCheck", Deductions: []scorecard.Deduction{scorecard.NewDeduction(10, "justification: %v", "value"), scorecard.NewDisqualification("disqualify: %v", "disqualification")}},
		{Name: "Check2", Deductions: []scorecard.Deduction{scorecard.NewDeduction(20, "justification: %v", "value")}},
		{Name: "DisqualifiedCheck2", Deductions: []scorecard.Deduction{scorecard.NewDeduction(20, "justification: %v", "value"), scorecard.NewDisqualification("disqualify: %v", "disqualification")}},
		{Name: "Check3", Deductions: []scorecard.Deduction{scorecard.NewDeduction(30, "justification: %v", "value")}},
		{Name: "DisqualifiedCheck3", Deductions: []scorecard.Deduction{scorecard.NewDeduction(30, "justification: %v", "value"), scorecard.NewDisqualification("disqualify: %v", "disqualification")}},
	}
	return scorecard.RunInfo{
		Groups: groups,
		Services: []scorecard.ServiceScore{
			{Path: "service", Score: scorecard.ServiceScorecard{Checks: serviceChecks, Average: 10}},
			{Path: "service2", Score: scorecard.ServiceScorecard{Checks: serviceChecks, Average: 10}},
		},
	}
}
