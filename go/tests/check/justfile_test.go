package check_test

import (
	"os"
	"path/filepath"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestJustfileFixtures(t *testing.T) {
	testCases := []struct {
		name          string
		justfile      *string
		expectedCount int
		expectedScore *int
	}{
			{name: "NoJustfile", justfile: nil, expectedCount: 4, expectedScore: testsupport.IntPtr(0)},
			{name: "BuildTestRunAndImport", justfile: strPtr("#!/usr/bin/env just --justfile\nimport 'vendir/justlib/just/base.just'\n\nbuild:\n\tgo build ./...\n\ntest:\n\tgo test ./...\n\nrun:\n\tgo run ./cmd/app\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
			{name: "BuildTestServeAndImport", justfile: strPtr("#!/usr/bin/env just --justfile\nimport 'vendir/justlib/just/base.just'\n\nbuild:\n\tgo build ./...\n\ntest:\n\tgo test ./...\n\nserve:\n\tair\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
			{name: "MissingBuild", justfile: strPtr("import 'vendir/justlib/just/base.just'\n\ntest:\n\tgo test ./...\n\nrun:\n\tgo run ./cmd/app\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
			{name: "MissingTest", justfile: strPtr("import 'vendir/justlib/just/base.just'\n\nbuild:\n\tgo build ./...\n\nrun:\n\tgo run ./cmd/app\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
			{name: "MissingRunAndServe", justfile: strPtr("import 'vendir/justlib/just/base.just'\n\nbuild:\n\tgo build ./...\n\ntest:\n\tgo test ./...\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(50)},
			{name: "MissingImport", justfile: strPtr("build:\n\tgo build ./...\n\ntest:\n\tgo test ./...\n\nrun:\n\tgo run ./cmd/app\n"), expectedCount: 1, expectedScore: testsupport.IntPtr(80)},
			{name: "MissingBuildAndImport", justfile: strPtr("test:\n\tgo test ./...\n\nrun:\n\tgo run ./cmd/app\n"), expectedCount: 2, expectedScore: testsupport.IntPtr(30)},
			{name: "CommentAfterRecipe", justfile: strPtr("import 'vendir/justlib/just/base.just'\nbuild: # Build it\n\tgo build ./...\n\ntest: # Test it\n\tgo test ./...\n\nrun: # Execute once\n\tgo run ./cmd/app\n"), expectedCount: 0, expectedScore: testsupport.IntPtr(100)},
	}

	check := checks.NewJustfile()
	for _, testCase := range testCases {
		t.Run(testCase.name, func(t *testing.T) {
			tempDir := t.TempDir()
			if err := os.Mkdir(filepath.Join(tempDir, ".git"), 0o755); err != nil {
				t.Fatal(err)
			}
			projectPath := filepath.Join(tempDir, "src", "project.csproj")
			testsupport.WriteFile(t, projectPath, "<Project></Project>")
			if testCase.justfile != nil {
				testsupport.WriteFile(t, filepath.Join(tempDir, "justfile"), *testCase.justfile)
			}
			testsupport.AssertFinalScore(t, check.Run(projectPath), testCase.expectedCount, testCase.expectedScore)
		})
	}
}
