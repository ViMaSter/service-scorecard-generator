## Getting Started with justlib

#### Just Documentation

[Docs](https://just.systems/man/en/)

#### JustLib Documentation

[Docs](https://l.aoe.com/justlib)

## Global Recipes

`just list`

Lists all available recipes
To make list the default recipe, add the following to your justfile:

    [private]
    default: list

---

`just choose`

Lets the user select the recipe to run. Uses fzf for fuzzy search of recipes.
To make list the default recipe, add the following to your justfile:

    [private]
    default: choose

---

`just selfupdate [vendir-file] [version-url]`

Performs a self update for justlib based on the latest version from given version-url. After updating
it also performs a sync to get the latest update for justlib core, recipes and modules.

---

`just x <command> <args>...`

Helper recipe to proxy a command through just.
This is helpful if a command requires environment variables that are only available within just.
Example:

    just x printenv

---

`just sync [<vendir-sync-argument>...]`

Synchronizes resources using vendir and runs some hooks afterward.
To enforce updates for e.g. images you can disable vendir's lazy loading by running `just sync --lazy=false`.

**Environment Variables**:

```shell
## Disables all hooks
JUST_VENDIR_HOOKS_ENABLED=false

## Excludes hooks from being executed. (comma separated list)
JUST_VENDIR_HOOKS_EXCLUDE="folder-name"
```

---

## Working with Modules

### Common Commands

Modules are printed as <module-name>... in the list of just recipes. To print a list of all module recipes,
please use:

`just <module-name>::list`

Prints a list of all available recipes

---

`just <module-name>::choose`

There is also an option to run a chooser (requires 'fzf' to be installed):

---

`just <module-name>::<recipe-name>`

Runs a module's recipe by name

---

`just <module-name>::help`

Prints the modules help/readme document

### Common Configuration

Modules can be configured using environment variables. To simplify this approach, one can use dotenv files.
Importing `env.just` file enables dotenv loading. It looks for a file named `.just.env` to load variables from.

_NOTE: This is not enabled by default due to the fact that settings in just cannot be overwritten._

**Example justfile**:

```just
#!/usr/bin/env just --justfile

import 'vendir/justlib/just/base.just'
import 'vendir/justlib/just/env.just'

mod example 'vendir/justlib/modules/example/module.just'
```

## Troubleshooting

### Debugging

Exporting or passing `DEBUG` environment variable to the command will enable debug logging. This may help
identifying issues.

Example:

```shell
DEBUG=true just sync
```
