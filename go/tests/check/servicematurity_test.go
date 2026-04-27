package check_test

import (
	"path/filepath"
	"strings"
	"testing"

	"github.com/vimaster/service-scorecard-generator/go/internal/checks"
	"github.com/vimaster/service-scorecard-generator/go/internal/scorecard"
	"github.com/vimaster/service-scorecard-generator/go/tests/testsupport"
)

func TestServiceMaturityFixtures(t *testing.T) {
	base := filepath.ToSlash(filepath.Join("fixtures", "Checks", "ServiceMaturity"))
	check := checks.NewServiceMaturity()

	testCases := []struct {
		name                 string
		fixture              string
		expectedScore        *int
		expectedLevelSnippet string
		expectedTarget       string
		expectCandidateNote  bool
	}{
		{
			name:                 "L0 infrastructure cluster",
			fixture:              "L0Infra",
			expectedScore:        testsupport.IntPtr(0),
			expectedLevelSnippet: "L0 - No customer n8n workload detected",
			expectedTarget:       "acme-corp/stackit-eu01-acme-corp-prod",
		},
		{
			name:                 "L1 legacy candidate",
			fixture:              "L1Candidate",
			expectedScore:        testsupport.IntPtr(34),
			expectedLevelSnippet: "L1 - Legacy operator/controller style n8n",
			expectedTarget:       "acme-corp/stackit-eu01-acme-corp-prod",
			expectCandidateNote:  true,
		},
		{
			name:                 "L1 legacy wired",
			fixture:              "L1Wired",
			expectedScore:        testsupport.IntPtr(34),
			expectedLevelSnippet: "L1 - Legacy operator/controller style n8n",
			expectedTarget:       "acme-corp/stackit-eu01-acme-corp-prod",
		},
		{
			name:                 "L2 manual",
			fixture:              "L2Manual",
			expectedScore:        testsupport.IntPtr(67),
			expectedLevelSnippet: "L2 - Manual n8n deployment",
			expectedTarget:       "acme-corp/stackit-eu01-acme-corp-prod",
		},
		{
			name:                 "L3 managed",
			fixture:              "L3Managed",
			expectedScore:        testsupport.IntPtr(100),
			expectedLevelSnippet: "L3 - managed-n8n Helm chart deployment",
			expectedTarget:       "acme-corp/stackit-eu01-acme-corp-prod",
		},
	}

	for _, testCase := range testCases {
		t.Run(testCase.name, func(t *testing.T) {
			repoRoot := newInfrastructureClusterWithFixture(t, filepath.ToSlash(filepath.Join(base, testCase.fixture)))
			deductions := check.Run(repoRoot)

			testsupport.AssertFinalScore(t, deductions, 1, testCase.expectedScore)
			justification := deductions[0].Justification
			if !strings.Contains(justification, testCase.expectedTarget) {
				t.Fatalf("expected justification to contain target %q, got %q", testCase.expectedTarget, justification)
			}
			if !strings.Contains(justification, testCase.expectedLevelSnippet) {
				t.Fatalf("expected justification to contain level snippet %q, got %q", testCase.expectedLevelSnippet, justification)
			}
			if testCase.expectCandidateNote && !strings.Contains(justification, "candidate") {
				t.Fatalf("expected candidate note in justification, got %q", justification)
			}
			if !testCase.expectCandidateNote && strings.Contains(justification, "candidate") {
				t.Fatalf("did not expect candidate note in justification, got %q", justification)
			}
		})
	}
}

func TestServiceMaturityDisqualifiesNonInfrastructureRepository(t *testing.T) {
	check := checks.NewServiceMaturity()
	repoRoot := t.TempDir()
	deductions := check.Run(repoRoot)

	if len(deductions) != 1 {
		t.Fatalf("expected 1 deduction, got %d", len(deductions))
	}
	if !deductions[0].IsDisqualification {
		t.Fatalf("expected disqualification deduction, got %+v", deductions[0])
	}
	if scorecard.CalculateFinalScore(deductions) != nil {
		t.Fatalf("expected final score nil for disqualification")
	}
}

func newInfrastructureClusterWithFixture(t *testing.T, fixturePath string) string {
	t.Helper()
	root := t.TempDir()
	repoRoot := filepath.Join(root, "customers", "acme-corp", "infrastructure", "stackit", "clusters", "stackit-eu01-acme-corp-prod")
	testsupport.FixtureCopyTree(t, fixturePath, repoRoot, nil)
	return repoRoot
}
