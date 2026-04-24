package checks

import (
	"bufio"
	"os"
	"path/filepath"
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type CodeOwners struct {
	baseCheck
}

func NewCodeOwners() *CodeOwners {
	return &CodeOwners{baseCheck: newBaseCheck("CodeOwners")}
}

func findRepoRoot(start string) string {
	dir := start
	if info, err := os.Stat(start); err != nil || !info.IsDir() {
		dir = filepath.Dir(start)
	}
	for {
		if _, err := os.Stat(filepath.Join(dir, ".git")); err == nil {
			return dir
		}
		parent := filepath.Dir(dir)
		if parent == dir {
			return start
		}
		dir = parent
	}
}

func countCodeOwners(path string) int {
	file, err := os.Open(path)
	if err != nil {
		return 0
	}
	defer file.Close()

	owners := make(map[string]struct{})
	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := strings.TrimSpace(scanner.Text())
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		fields := strings.Fields(line)
		for _, field := range fields[1:] {
			owners[field] = struct{}{}
		}
	}
	return len(owners)
}

func (c *CodeOwners) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	repoRoot := findRepoRoot(absolutePathToProjectFile)
	codeOwnersPath := filepath.Join(repoRoot, "CODEOWNERS")

	if _, err := os.Stat(codeOwnersPath); os.IsNotExist(err) {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "No CODEOWNERS file found at %v", codeOwnersPath)}
	}

	ownerCount := countCodeOwners(codeOwnersPath)
	if ownerCount == 0 {
		return []scorecard.Deduction{scorecard.NewDeduction(100, "CODEOWNERS file at %v has no owners defined", codeOwnersPath)}
	}
	if ownerCount == 1 {
		return []scorecard.Deduction{scorecard.NewDeduction(50, "CODEOWNERS file at %v has only 1 owner defined", codeOwnersPath)}
	}
	return nil
}

var _ Check = (*CodeOwners)(nil)
