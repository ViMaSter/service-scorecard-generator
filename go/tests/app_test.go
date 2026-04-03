package tests

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/app"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestListChecksMatchesExpectedOrder(t *testing.T) {
	generator := app.Generator{Now: func() time.Time { return time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC) }}
	output, err := generator.ListChecks("")
	if err != nil {
		t.Fatal(err)
	}
	expected := "Available checks:\n  - BuiltForAKS\n  - HintPathCounter\n  - ImplicitAssemblyInfo\n  - LatestNET\n  - NullableSetup\n  - PendingRenovateAzurePRs\n  - ProperDockerfile"
	if output != expected {
		t.Fatalf("unexpected list-checks output\nexpected:\n%s\nactual:\n%s", expected, output)
	}
}

func TestExecuteCreatesConfigAndMkDocsOutput(t *testing.T) {
	root := t.TempDir()
	projectDirectory := filepath.Join(root, "service")
	testsupport.WriteFile(t, filepath.Join(projectDirectory, "service.csproj"), `<Project Sdk="Microsoft.NET.Sdk.Web"><PropertyGroup><TargetFramework>net10.0</TargetFramework><Nullable>enable</Nullable><GenerateAssemblyInfo>true</GenerateAssemblyInfo><Company>x</Company><Copyright>x</Copyright><Description>x</Description><FileVersion>x</FileVersion><InformalVersion>x</InformalVersion><Product>x</Product><UserSecretsId>x</UserSecretsId></PropertyGroup></Project>`)
	testsupport.WriteFile(t, filepath.Join(projectDirectory, "azure-pipelines.yml"), "")
	testsupport.WriteFile(t, filepath.Join(projectDirectory, "Dockerfile"), "dotnet build\ndotnet sonarscanner")
	generator := app.Generator{WorkingDir: root, Now: func() time.Time { return time.Date(2026, time.April, 3, 12, 0, 0, 0, time.UTC) }}
	outputDirectory := filepath.Join(root, "out")
	if err := generator.Execute(outputDirectory, "mkdocsmarkdown", "", ""); err != nil {
		t.Fatal(err)
	}
	if _, err := os.Stat(filepath.Join(root, "scorecard.config.json")); err != nil {
		t.Fatal(err)
	}
	content := testsupport.ReadFile(t, filepath.Join(outputDirectory, "Service-Scorecard.md"))
	if !strings.Contains(content, "Service Scorecard for 2026-04-03") || !strings.Contains(content, "service") {
		t.Fatalf("unexpected generated output: %s", content)
	}
}
