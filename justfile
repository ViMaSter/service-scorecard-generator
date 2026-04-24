#!/usr/bin/env just --justfile

# import 'vendir/justlib/just/base.just'

# Default recipe, that gets invoked if just is called without any argument
# [private]
# default: list

# Build the binary
build:
    cd go && go build -o bin/ScorecardGenerator ./cmd/scorecardgenerator

# Run all tests
test:
    cd go && go test ./... -v

# Run the report generator. Usage: just run <PAT>
run pat:
    GITLAB_PAT={{pat}} ./integrationtest_go/run_mkdocs_scan.sh