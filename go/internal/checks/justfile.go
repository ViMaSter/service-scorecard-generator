package checks

import (
	"os"
	"path/filepath"
	"regexp"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

const justfileBaseImport = "import 'vendir/justlib/just/base.just'"

var justRecipePattern = regexp.MustCompile(`(?m)^\s*([a-zA-Z0-9_-]+):\s*(?:#.*)?$`)

type Justfile struct {
	baseCheck
}

func NewJustfile() *Justfile {
	return &Justfile{baseCheck: newBaseCheck("Justfile")}
}

func hasJustRecipe(content string, recipeName string) bool {
	matches := justRecipePattern.FindAllStringSubmatch(content, -1)
	for _, match := range matches {
		if len(match) > 1 && match[1] == recipeName {
			return true
		}
	}
	return false
}

func (c *Justfile) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	repoRoot := findRepoRoot(absolutePathToProjectFile)
	justfilePath := filepath.Join(repoRoot, "justfile")

	contentBytes, err := os.ReadFile(justfilePath)
	if err != nil {
		return []scorecard.Deduction{
			scorecard.NewDeduction(50, "justfile not found at %v: missing 'build:' recipe", relPath(justfilePath)),
			scorecard.NewDeduction(50, "justfile not found at %v: missing 'test:' recipe", relPath(justfilePath)),
			scorecard.NewDeduction(50, "justfile not found at %v: missing either 'run:' or 'serve:' recipe", relPath(justfilePath)),
			scorecard.NewDeduction(20, "justfile not found at %v: missing %q", relPath(justfilePath), justfileBaseImport),
		}
	}

	content := string(contentBytes)
	deductions := make([]scorecard.Deduction, 0, 4)
	if !hasJustRecipe(content, "build") {
		deductions = append(deductions, scorecard.NewDeduction(50, "%v is missing 'build:' recipe", relPath(justfilePath)))
	}
	if !hasJustRecipe(content, "test") {
		deductions = append(deductions, scorecard.NewDeduction(50, "%v is missing 'test:' recipe", relPath(justfilePath)))
	}
	if !hasJustRecipe(content, "run") && !hasJustRecipe(content, "serve") {
		deductions = append(deductions, scorecard.NewDeduction(50, "%v is missing either 'run:' or 'serve:' recipe", relPath(justfilePath)))
	}
	if !regexp.MustCompile(regexp.QuoteMeta(justfileBaseImport)).MatchString(content) {
		deductions = append(deductions, scorecard.NewDeduction(20, "%v is missing %q", relPath(justfilePath), justfileBaseImport))
	}
	return deductions
}

var _ Check = (*Justfile)(nil)
