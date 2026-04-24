# Justfile

Checks whether a repository root `justfile` exists and contains these required items:

- A `build:` recipe
- A `test:` recipe
- `import 'vendir/justlib/just/base.just'`

Scoring:

- Start at 100 points
- Deduct 50 points if `build:` is missing
- Deduct 50 points if `test:` is missing
- Deduct 20 points if the vendir import is missing
