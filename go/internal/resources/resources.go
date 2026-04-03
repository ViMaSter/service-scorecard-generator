package resources

import "embed"

//go:embed checks/*.md
var CheckReadmes embed.FS

//go:embed configuration/default.json
var DefaultConfigJSON string

//go:embed visualizer/HTMLVisualizer.html
var HTMLVisualizerTemplate string
