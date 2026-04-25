#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

REPOSITORY_ROOT="${REPOSITORY_ROOT:?Error: REPOSITORY_ROOT env var is required}"
SCORECARD_BIN="$SCRIPT_DIR/../go/bin/ScorecardGenerator"
VISUALIZER="${VISUALIZER:-mkdocsmarkdown}"

timestamp="$(date +"%Y%m%d-%H%M%S")"
run_dir="$PWD/run"

if ! command -v ghorg >/dev/null 2>&1; then
  echo "Error: ghorg is not installed or not in PATH." >&2
  exit 1
fi

if [[ ! -f "$SCORECARD_BIN" ]]; then
  echo "Error: ScorecardGenerator binary not found at: $SCORECARD_BIN" >&2
  exit 1
fi

mkdir -p "$run_dir"
echo "Created run directory: $run_dir"

sources_dir="$REPOSITORY_ROOT"
if [[ ! -d "$sources_dir" ]]; then
  echo "Error: source directory not found at: $sources_dir" >&2
  exit 1
fi

echo "Running ScorecardGenerator from: $sources_dir"
(
  cd "$sources_dir"
  "$SCORECARD_BIN" --output-path "$run_dir" --visualizer "$VISUALIZER"
)

echo "Done. Generated files at"
echo "$run_dir"