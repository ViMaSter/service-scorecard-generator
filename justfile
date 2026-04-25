#!/usr/bin/env just --justfile

import 'vendir/justlib/just/base.just'

# Default recipe, that gets invoked if just is called without any argument
[private]
default: list

# Build the binary
build:
    cd {{justfile_directory()}}/go && go build -o bin/ScorecardGenerator ./cmd/scorecardgenerator

# Run all tests
test:
    cd {{justfile_directory()}}/go && go test ./... -v

# Generate a mkdocs compatible report
run path:
    REPOSITORY_ROOT={{path}} {{justfile_directory()}}/integrationtest_go/run_mkdocs_scan.sh