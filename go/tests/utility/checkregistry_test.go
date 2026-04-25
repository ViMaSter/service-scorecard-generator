package utility_test

import (
	"strings"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkregistry"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestRegistryContainsExpectedChecks(t *testing.T) {
	registry := checkregistry.Registry(checkregistry.Config{})
	expectedNames := expectedRegistryCheckNames(t)

	if len(registry) != len(expectedNames) {
		t.Fatalf("expected %d checks in registry, got %d", len(expectedNames), len(registry))
	}

	for _, checkName := range expectedNames {
		factory, ok := registry[checkName]
		if !ok {
			t.Fatalf("missing check %q in registry", checkName)
		}
		check := factory()
		if check == nil {
			t.Fatalf("check factory for %q returned nil", checkName)
		}
		if check.Name() != checkName {
			t.Fatalf("expected check name %q, got %q", checkName, check.Name())
		}
	}
}

func expectedRegistryCheckNames(t *testing.T) []string {
	t.Helper()
	content := testsupport.FixtureRead(t, "fixtures/Checks/Registry/ExpectedCheckNames.txt")
	var names []string
	for _, line := range strings.Split(content, "\n") {
		trimmed := strings.TrimSpace(line)
		if trimmed == "" || strings.HasPrefix(trimmed, "#") {
			continue
		}
		names = append(names, trimmed)
	}
	if len(names) == 0 {
		t.Fatal("expected at least one check name in fixture")
	}
	return names
}

func TestRegistryChecksHaveInfoPageContent(t *testing.T) {
	registry := checkregistry.Registry(checkregistry.Config{})

	for checkName, factory := range registry {
		check := factory()
		if check.InfoPageContent() == "" {
			t.Fatalf("expected check %q to have non-empty InfoPageContent", checkName)
		}
	}
}
