package visualizer

import "github.com/vimaster/service-scorecard-generator/go/internal/scorecard"

type Visualizer interface {
	Visualize(runInfo scorecard.RunInfo) error
}
