# This file is meant to be imported in modules.
# It comes with some handy standard recipes that makes it easier to work with modules.
# Usage:
#   import 'just/module.just'
#
# Once imported, modules can be imported into a justfile and used as follows:
# -- justfile
# mod <module-name> 'path/to/module.just'
#
# -- CLI
#   just <module-name>::list
#   just <module-name>::readme

import 'partial/settings.just'
import 'partial/vars.just'
import 'partial/utils.just'

# Should be overwriten by module to list its recipes
# NOTE: This variable must be copied/overwritten in modules to function.

module-path := source_file()

# Holds the modules directory path

module-dir := parent_directory(module-path)

# Lists all available/public recipes.
[group('module')]
@list: (just-list module-path)

# Lets the user select the recipe to run. Uses fzf for fuzzy search of recipes.
[group('module')]
@choose: (just-choose module-path)

# Prints a help.md file to console if available
[group('module')]
@help: (print-markdown (module-dir + "/help.md"))
