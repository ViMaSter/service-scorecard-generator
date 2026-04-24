# NoDSStore

## About
This check returns **100 points** when Git tracks no `.DS_Store` file and `.gitignore` globally ignores `.DS_Store`.

Applies a **100 point** deduction if Git tracks any `.DS_Store` file.
Applies a **50 point** deduction if `.gitignore` doesn't globally ignore `.DS_Store`.

## How to achieve 100 points
Delete all tracked `.DS_Store` files and add a global ignore rule in the repository `.gitignore`, for example:

```
.DS_Store
```

## Why this check exists
`.DS_Store` is a macOS Finder metadata file and has no use for source code on Unix or Windows systems.
Ignoring it globally keeps repositories clean and prevents accidental commits across platforms.