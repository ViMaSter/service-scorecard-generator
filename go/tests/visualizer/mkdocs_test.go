package visualizer_test

import (
	"path/filepath"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/visualizer"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestMkDocsVisualizerMatchesCSharpFixture(t *testing.T) {
	now := time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC)
	outputDirectory := t.TempDir()
	v := visualizer.NewMkDocsMarkdownVisualizer(outputDirectory, func() time.Time { return now })
	if err := v.Visualize(testsupport.SampleRunInfo()); err != nil {
		t.Fatal(err)
	}
	actual := testsupport.Normalized(testsupport.ReadFile(t, filepath.Join(outputDirectory, "Service-Scorecard.md")))
	expected := testsupport.Normalized(testsupport.ReplaceTodayPlaceholder(testsupport.FixtureRead(t, filepath.ToSlash(filepath.Join("fixtures", "Visualizer", "MkDocsMarkdownVisualizer", "WithoutGitRepo", "Service-Scorecard.md"))), now))
	if actual != expected {
		t.Fatalf("mkdocs output mismatch\nexpected:\n%s\nactual:\n%s", expected, actual)
	}
}
