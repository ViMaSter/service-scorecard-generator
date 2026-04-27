package main

import (
	"flag"
	"fmt"
	"os"
	"time"

	"github.com/vimaster/service-scorecard-generator/go/internal/app"
)

func main() {
	generator := app.Generator{Now: time.Now}
	if len(os.Args) > 1 && os.Args[1] == "list-checks" {
		output, err := generator.ListChecks("")
		if err != nil {
			fmt.Fprintln(os.Stderr, err)
			os.Exit(1)
		}
		fmt.Printf("[%s INF][] %s\n", time.Now().Format("15:04:05"), output)
		return
	}
	flags := flag.NewFlagSet(os.Args[0], flag.ExitOnError)
	outputPath := flags.String("output-path", "", "Output directory")
	visualizerName := flags.String("visualizer", "", "Visualizer name")
	excludePath := flags.String("exclude-path", "", "Exclude path substring")
	pat := flags.String("pat", "", "Personal access token")
	legacyAzurePAT := flags.String("azure-pat", "", "Deprecated alias for --pat")
	_ = flags.Parse(os.Args[1:])
	effectivePAT := *pat
	if effectivePAT == "" {
		effectivePAT = *legacyAzurePAT
	}
	if *outputPath == "" || *visualizerName == "" {
		fmt.Fprintln(os.Stderr, "output-path and visualizer are required")
		os.Exit(1)
	}
	if err := generator.Execute(*outputPath, *visualizerName, *excludePath, effectivePAT); err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
}
