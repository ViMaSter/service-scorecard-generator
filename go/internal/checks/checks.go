package checks

import (
	"fmt"
	"io/fs"
	"os"
	"path/filepath"

	"github.com/vimaster/service-scorecard-generator/go/internal/resources"
	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
)

type Check interface {
	Name() string
	InfoPageContent() string
	Run(absolutePathToProjectFile string) []scorecard.Deduction
}

type baseCheck struct {
	name            string
	infoPageContent string
}

func newBaseCheck(name string) baseCheck {
	content, err := fs.ReadFile(resources.CheckReadmes, filepath.ToSlash(filepath.Join("checks", name+".md")))
	if err != nil {
		panic(fmt.Sprintf("check %s must have README.md content: %v", name, err))
	}
	return baseCheck{name: name, infoPageContent: string(content)}
}

func (b baseCheck) Name() string {
	return b.name
}

func (b baseCheck) InfoPageContent() string {
	return b.infoPageContent
}

func relPath(absolutePath string) string {
	cwd, err := os.Getwd()
	if err != nil {
		return absolutePath
	}
	rel, err := filepath.Rel(cwd, absolutePath)
	if err != nil {
		return absolutePath
	}
	return rel
}
