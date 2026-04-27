package checks

import (
	"os"
	"path/filepath"
	"strings"

	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/internal/utility/checkruntime"
)

type ServiceMaturity struct {
	checkruntime.BaseCheck
}

type serviceMaturityInput struct {
	RepoRoot                string
	Customer                string
	Cluster                 string
	IsInfrastructureCluster bool
	HasLegacyArtifact       bool
	HasLegacyWiring         bool
	HasManualDeployment     bool
	HasManagedHelmChart     bool
}

func NewServiceMaturity() *ServiceMaturity {
	return &ServiceMaturity{BaseCheck: checkruntime.NewBaseCheck("ServiceMaturity")}
}

func (c *ServiceMaturity) Run(absolutePathToProjectFile string) []scorecard.Deduction {
	input := collectServiceMaturityInput(absolutePathToProjectFile)
	level := maturityLevel(input)
	title := "No customer n8n workload detected"
	note := ""

	switch level {
	case 1:
		title = "Legacy operator/controller style n8n"
		if !input.HasLegacyWiring {
			note = " (candidate: legacy n8n artifact exists, but appears not wired from bootstrap)"
		}
	case 2:
		title = "Manual n8n deployment"
	case 3:
		title = "managed-n8n Helm chart deployment"
	}

	target := input.RepoRoot
	if input.IsInfrastructureCluster {
		target = filepath.ToSlash(filepath.Join(input.Customer, input.Cluster))
	}

	message := "Service maturity for %v: L%v - %v%v"
	return []scorecard.Deduction{scorecard.NewDeduction(0, message, target, level, title, note)}
}

func collectServiceMaturityInput(repoRoot string) serviceMaturityInput {
	input := serviceMaturityInput{RepoRoot: repoRoot}
	path := filepath.ToSlash(repoRoot)
	parts := strings.Split(path, "/")

	for i := 0; i+4 < len(parts); i++ {
		if parts[i] == "customers" && parts[i+2] == "infrastructure" && parts[i+4] == "clusters" && i+5 < len(parts) {
			input.Customer = parts[i+1]
			input.Cluster = parts[i+5]
			input.IsInfrastructureCluster = true
			break
		}
	}

	if !input.IsInfrastructureCluster {
		return input
	}

	input.HasLegacyArtifact = hasAnyPath(repoRoot,
		"gitops/managed-services-config/n8n/n8n.yaml",
		"gitops/managed-services-config/n8n/kustomization.yaml",
	)
	input.HasLegacyWiring = hasAnyPath(repoRoot,
		"gitops/bootstrap/base/managed-services-config.yaml",
		"gitops/bootstrap/base/managed-services.yaml",
	)
	input.HasManagedHelmChart = hasAnyPath(repoRoot,
		"gitops/custom-services/deployment/n8n/n8n-helmrelease.yaml",
		"gitops/custom-services/deployment/n8n-dev/n8n-dev-helmrelease.yaml",
	) && hasAnyPath(repoRoot,
		"gitops/custom-services/dependencies/n8n/repository/helm-repository.yaml",
	)
	input.HasManualDeployment = hasAnyPath(repoRoot,
		"gitops/custom-services/deployment/n8n/deployment.yaml",
		"gitops/custom-services/deployment/n8n/service.yaml",
		"gitops/custom-services/deployment/n8n/ingress.yaml",
	)

	return input
}

func hasAnyPath(repoRoot string, relativePaths ...string) bool {
	for _, relativePath := range relativePaths {
		if _, err := os.Stat(filepath.Join(repoRoot, filepath.FromSlash(relativePath))); err == nil {
			return true
		}
	}
	return false
}

func maturityLevel(input serviceMaturityInput) int {
	switch {
	case atLeastLevel3(input):
		return 3
	case atLeastLevel2(input):
		return 2
	case atLeastLevel1(input):
		return 1
	case atLeastLevel0(input):
		return 0
	default:
		return 0
	}
}

func atLeastLevel0(input serviceMaturityInput) bool {
	return baseMaturityLevel(input) >= 0
}

func atLeastLevel1(input serviceMaturityInput) bool {
	return baseMaturityLevel(input) >= 1
}

func atLeastLevel2(input serviceMaturityInput) bool {
	return baseMaturityLevel(input) >= 2
}

func atLeastLevel3(input serviceMaturityInput) bool {
	return baseMaturityLevel(input) >= 3
}

func baseMaturityLevel(input serviceMaturityInput) int {
	if !input.IsInfrastructureCluster {
		return 0
	}
	if input.HasManagedHelmChart {
		return 3
	}
	if input.HasManualDeployment {
		return 2
	}
	if input.HasLegacyArtifact {
		return 1
	}
	return 0
}

var _ checkruntime.Check = (*ServiceMaturity)(nil)
