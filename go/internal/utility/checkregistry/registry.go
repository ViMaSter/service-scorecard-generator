package checkregistry

import (
	"net/http"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type Config struct {
	PAT    string
	Client   *http.Client
}

func Registry(config Config) map[string]func() checkruntime.Check {
	return map[string]func() checkruntime.Check{
		"BuiltForAKS":             func() checkruntime.Check { return checks.NewBuiltForAKS() },
		"CodeOwners":              func() checkruntime.Check { return checks.NewCodeOwners() },
		"HintPathCounter":         func() checkruntime.Check { return checks.NewHintPathCounter() },
		"ImplicitAssemblyInfo":    func() checkruntime.Check { return checks.NewImplicitAssemblyInfo() },
		"Justfile":                func() checkruntime.Check { return checks.NewJustfile() },
		"LatestNET":               func() checkruntime.Check { return checks.NewLatestNET(config.Client) },
		"NoDSStore":               func() checkruntime.Check { return checks.NewNoDSStore() },
		"NullableSetup":           func() checkruntime.Check { return checks.NewNullableSetup() },
		"PendingRenovateAzurePRs": func() checkruntime.Check { return checks.NewPendingRenovateAzurePRs(config.PAT, config.Client) },
		"PendingRenovateGitLabPRs": func() checkruntime.Check { return checks.NewPendingRenovateGitLabPRs(config.PAT, config.Client) },
		"ProperDockerfile":        func() checkruntime.Check { return checks.NewProperDockerfile() },
	}
}
