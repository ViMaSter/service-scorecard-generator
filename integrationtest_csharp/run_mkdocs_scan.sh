#!/usr/bin/env bash
set -euo pipefail

# Defaults requested by user.
GITLAB_GROUP="scorecardgenerator"
GITLAB_PAT="${GITLAB_PAT:-glpat-wg62IWtSAkHgNx7IV7UaDmM6MQpvOjEKdTpscDE5NA8.01.170wyckfz}"
SCORECARD_BIN="/Users/vincent.mahnke/service-scorecard-generator/ScorecardGenerator/bin/Debug/net7.0/ScorecardGenerator"
VISUALIZER="${VISUALIZER:-mkdocsmarkdown}"

timestamp="$(date +"%Y%m%d-%H%M%S")"
run_dir="$PWD/run-$timestamp"

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

echo "Cloning GitLab group repositories..."
ghorg clone "$GITLAB_GROUP" \
  --scm gitlab \
  --token "$GITLAB_PAT" \
  --path "$run_dir" \
  --output-dir "$GITLAB_GROUP" \
  --preserve-scm-hostname \
  --preserve-dir

sources_dir=""
if [[ -d "$run_dir/gitlab.com/$GITLAB_GROUP" ]]; then
  sources_dir="$run_dir/gitlab.com/$GITLAB_GROUP"
elif [[ -d "$run_dir/$GITLAB_GROUP" ]]; then
  sources_dir="$run_dir/$GITLAB_GROUP"
else
  echo "Error: could not find cloned source directory under $run_dir" >&2
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