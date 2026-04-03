package visualizer_test

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/visualizer"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestGitLabVisualizerMatchesCSharpFixture(t *testing.T) {
	now := time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC)
	outputDirectory := t.TempDir()
	v := visualizer.NewGitLabMarkdownVisualizer(outputDirectory, func() time.Time { return now })
	if err := v.Visualize(testsupport.SampleRunInfo()); err != nil {
		t.Fatal(err)
	}
	mainFile := testsupport.Normalized(testsupport.ReadFile(t, filepath.Join(outputDirectory, "Service-Scorecard.md")))
	for _, expected := range []string{"# Service Scorecard for 2026-04-03", "service", "service2", "Check", "DisqualifiedCheck", "[[_TOSP_]]", "\"ServiceScores\""} {
		if !strings.Contains(mainFile, expected) {
			t.Fatalf("expected gitlab output to contain %q", expected)
		}
	}
	for _, checkFile := range []string{"Check.md", "DisqualifiedCheck.md", "Check2.md", "DisqualifiedCheck2.md", "Check3.md", "DisqualifiedCheck3.md"} {
		if _, err := os.Stat(filepath.Join(outputDirectory, "Service-Scorecard", checkFile)); err != nil {
			t.Fatalf("missing check page %s", checkFile)
		}
	}
}
