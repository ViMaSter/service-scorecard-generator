#!/usr/bin/env just --justfile

import 'vendir/justlib/just/base.just'

# Default recipe, that gets invoked if just is called without any argument
[private]
default: list

# Build the binary
build:
    go build -o renovate-report .

# Run all tests
test:
    go test ./... -v

# Run the report generator. Usage: just run <PAT>
run pat:
    GITLAB_TOKEN={{pat}} go run .