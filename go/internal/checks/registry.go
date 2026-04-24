package checks

import "net/http"

type RegistryConfig struct {
	AzurePAT string
	Client   *http.Client
}

func Registry(config RegistryConfig) map[string]func() Check {
	return map[string]func() Check{
		"BuiltForAKS":             func() Check { return NewBuiltForAKS() },
		"CodeOwners":              func() Check { return NewCodeOwners() },
		"HintPathCounter":         func() Check { return NewHintPathCounter() },
		"ImplicitAssemblyInfo":    func() Check { return NewImplicitAssemblyInfo() },
		"Justfile":                func() Check { return NewJustfile() },
		"LatestNET":               func() Check { return NewLatestNET(config.Client) },
		"NullableSetup":           func() Check { return NewNullableSetup() },
		"PendingRenovateAzurePRs": func() Check { return NewPendingRenovateAzurePRs(config.AzurePAT, config.Client) },
		"ProperDockerfile":        func() Check { return NewProperDockerfile() },
	}
}
