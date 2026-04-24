#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

GITLAB_GROUP="${GITLAB_GROUP:?Error: GITLAB_GROUP env var is required}"
GITLAB_PAT="${GITLAB_PAT:?Error: GITLAB_PAT env var is required}"
SCORECARD_BIN="$SCRIPT_DIR/../go/bin/ScorecardGenerator"
VISUALIZER="${VISUALIZER:-mkdocsmarkdown}"

timestamp="$(date +"%Y%m%d-%H%M%S")"
run_dir="$PWD/run-$timestamp"

# Keep both variable names for tooling compatibility.
export GITLAB_PAT
export GITLAB_TOKEN="$GITLAB_PAT"

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

sources_dir="$HOME/ghorg/$GITLAB_GROUP"
if [[ ! -d "$sources_dir" ]]; then
  echo "Error: source directory not found at: $sources_dir" >&2
  exit 1
fi

wiki_dir="$run_dir/wiki"
mkdir -p "$wiki_dir"
echo "Created wiki output directory: $wiki_dir"

echo "Running ScorecardGenerator from: $sources_dir"
(
  cd "$sources_dir"
  "$SCORECARD_BIN" --output-path "$wiki_dir" --visualizer "$VISUALIZER"
)

echo "Done."
echo "Run directory: $run_dir"
echo "Wiki output:   $wiki_dir"
