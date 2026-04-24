# Justfile

## About
This check returns **100 points** if the repository root contains a `justfile` with all required items:

- A `build:` recipe
- A `test:` recipe
- Either
    - A `run:` recipe **or**
    - A `serve:` recipe
- `import 'vendir/justlib/just/base.just'`

**50 points** are deducted if `build:` is missing.  
**50 points** are deducted if `test:` is missing.  
**50 points** are deducted if both `run:` and `serve:` are missing.  
**20 points** are deducted if the `vendir` import is missing.

## How to achieve 100 points
Create a `justfile` in the root of the repository that defines a `build:` recipe, a `test:` recipe, and either a `run:` recipe (for one-time execution) or a `serve:` recipe (for local watch-mode workflows), and imports the shared library with `import 'vendir/justlib/just/base.just'`.

Also avoid `just dev`: it is not grammatically correct because recipe names should be verbs. Prefer `just run` or `just serve`.

## Why this check exists
A `justfile` lets developers run the exact same commands and binaries locally that GitLab CI/CD relies on.  
This eliminates "works on my machine" problems, reduces the friction of onboarding, and ensures that local builds and test runs are reproducible and consistent with the pipeline.
