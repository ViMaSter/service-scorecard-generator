package visualizer_test

import (
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/visualizer"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestHTMLVisualizerRendersExpectedSections(t *testing.T) {
	now := time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC)
	outputDirectory := t.TempDir()
	v := visualizer.NewHTMLVisualizer(outputDirectory, func() time.Time { return now })
	if err := v.Visualize(testsupport.SampleRunInfo()); err != nil {
		t.Fatal(err)
	}
	content := testsupport.Normalized(testsupport.ReadFile(t, filepath.Join(outputDirectory, "index.html")))
	for _, expected := range []string{"Service Scorecard for 2026-04-03", "Check", "DisqualifiedCheck", "PageContent", "service2"} {
		if !strings.Contains(content, expected) {
			t.Fatalf("expected html output to contain %q", expected)
		}
	}
}
