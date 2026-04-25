package checks

import (
	"bytes"
	"os"
	"os/exec"
	"path/filepath"
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type NoDSStore struct {
	checkruntime.BaseCheck
}

func NewNoDSStore() *NoDSStore {
	return &NoDSStore{BaseCheck: checkruntime.NewBaseCheck("NoDSStore")}
}

func (c *NoDSStore) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	repoRoot := findRepoRoot(absolutePathToProjectFile)
	deductions := make([]scorecard.Deduction, 0, 2)

	if foundPath := findFirstTrackedDSStore(repoRoot); foundPath != "" {
		deductions = append(deductions, scorecard.NewDeduction(100, "Found tracked %v; .DS_Store files must not be tracked in repositories", checkruntime.RelPath(foundPath)))
	}

	gitIgnorePath := filepath.Join(repoRoot, ".gitignore")
	if !gitignoreGloballyIgnoresDSStore(gitIgnorePath) {
		deductions = append(deductions, scorecard.NewDeduction(50, "%v does not globally ignore .DS_Store", checkruntime.RelPath(gitIgnorePath)))
	}

	return deductions
}

func findFirstTrackedDSStore(root string) string {
	output, err := exec.Command("git", "-C", root, "ls-files", "--cached").Output()
	if err != nil {
		return ""
	}
	for _, rawLine := range bytes.Split(output, []byte{'\n'}) {
		trackedPath := strings.TrimSpace(string(rawLine))
		if trackedPath == ".DS_Store" || strings.HasSuffix(trackedPath, "/.DS_Store") {
			return filepath.Join(root, filepath.FromSlash(trackedPath))
		}
	}
	return ""
}

func gitignoreGloballyIgnoresDSStore(path string) bool {
	content, err := os.ReadFile(path)
	if err != nil {
		return false
	}
	for _, rawLine := range strings.Split(string(content), "\n") {
		line := strings.TrimSpace(rawLine)
		if line == "" || strings.HasPrefix(line, "#") || strings.HasPrefix(line, "!") {
			continue
		}
		switch line {
		case ".DS_Store", "/.DS_Store", "**/.DS_Store", "*.DS_Store":
			return true
		}
	}
	return false
}

var _ checkruntime.Check = (*NoDSStore)(nil)