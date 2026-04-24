## This file is meant to be included in "justfile" files.
## It defines some settings and global variables.

import 'partial/settings.just'
import 'partial/vars.just'
import 'partial/utils.just'

vendir-path := join(justfile_directory(), "vendir")

########################################################################################################################
# Common recipes
########################################################################################################################

# Lists all available/public recipes.
[group('global')]
@list: just-list

# Lets the user select the recipe to run. Uses fzf for fuzzy search of recipes.
[group('global')]
@choose: just-choose

# Hepler recipe to proxy a command through just.
[group('global')]
@x command *args:
    {{ command }} {{ args }}

# Opens just online documentation in your default browser
[group('global')]
sync *args='': (vendir-sync args)

# Synchronizes vendir and runs just hooks
[private]
vendir-sync *args='': (requires "vendir")
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    @info "Running vendir sync. This might take a little ..."
    @debug "vendir sync args: {{ args }}"

    # If debug is enabled, then run vendir sync without modifications to the output
    if [ -n "${DEBUG}" ]; then
      vendir sync {{ args }} || exit 1
    else
      ## Create a cache directory
      mkdir -p ~/.vendir/cache

      ## Run vendir sync, but hide stdout as it is pretty chatty
      VENDIR_CACHE_DIR=~/.vendir/cache vendir sync {{ args }} 1> /dev/null || exit 1

      ## Print lock file as a result
      @hr
      cat vendir.lock.yml
      @hr
    fi

    @colorize "green" "Successfully synced vendir resources!"

    # Run hooks in new environment to ensure the latest synced version of justlib is used for hooks
    @recipe vendir-sync-hooks

# Allows to disable hooks on sync

vendir-hooks-enabled := env("JUST_VENDIR_HOOKS_ENABLED", "true")

# Commata separated list of folders, which should be excluded from hook execution

vendir-hooks-exclude := env("JUST_VENDIR_HOOKS_EXCLUDE", "")

# Runs vendir hooks
[private]
vendir-sync-hooks path=vendir-path:
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    # Hooks are disabled
    if [ "{{ vendir-hooks-enabled }}" == "false" ]; then
      @debug "Skipping hooks as JUST_VENDIR_HOOKS_ENABLED is set to false"
      exit 0
    fi

    # Allow to specifiy some config parameters as environment variables
    justfile="${JUST_VENDIR_FILE:-.vendir.just}"

    # Find hook files and append them to hook array
    hooks=()
    while IFS=  read -r -d $'\0'; do
      hooks+=("$REPLY")
    done < <(find "{{ path }}" -type f -name "$justfile" -print0)

    # Check if there are hooks to run
    if [ -z "${hooks}" ]; then
      @debug "No hooks found in {{ path }}"
      exit 0
    fi

    @info "Running vendir hooks...\n"

    # Convert exclusion list into array
    exclusions=()
    IFS=', ' read -r -a exclusions <<< "{{ vendir-hooks-exclude }}"

    @debug "Got exclusion list: ${exclusions[@]}"

    # Loop hooks
    for hook in "${hooks[@]}"; do
      hook_name=$(basename $(dirname $hook))

      if [[ " ${exclusions[*]} " =~ [[:space:]]${hook_name}[[:space:]] ]]; then
          @debug "Skipping hook $(basename $(dirname $hook))"
          continue
      fi

      echo -e "Running ${justfile} found in ${hook_name}"
      @hr
      just -d "{{ justfile_directory() }}" -f "${hook}" || exit 1
      @hr
      @break
    done

# Checks for the latest version of justlib and updates it in vendir.yml accordingly
[group('global')]
selfupdate vendir-file='vendir.yml' version-url='https://l.aoe.com/justlib-version-latest':
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    # Configuration
    YAML_FILE="{{ vendir-file }}"  # Path to the YAML file
    VERSION_URL="{{ version-url }}"  # URL to fetch the latest version

    # Check if required tools are installed
    if ! command -v curl &> /dev/null; then
        @fatal "curl is not installed. Please install it and try again."
    fi

    if ! command -v yq &> /dev/null; then
        @fatal "yq is not installed. Please install it and try again."
    fi

    # Fetch the latest version using curl
    @info "Fetching latest version from $VERSION_URL..."
    LATEST_VERSION=$(curl -fsSL "$VERSION_URL")

    if [[ -z "$LATEST_VERSION" ]]; then
        @fatal "Failed to fetch the latest version."
    fi
    @info "Latest version: $LATEST_VERSION"

    # Get the current version from the YAML file
    CURRENT_VERSION=$(yq e '
      .directories[].contents[]
      | select(.path == "justlib").git.ref' "$YAML_FILE")

    if [[ -z "$CURRENT_VERSION" ]]; then
        @fatal "Could not find the current version for justlib in $YAML_FILE."
    fi
    @info "Current version: $CURRENT_VERSION"

    # Check if an update is needed
    if [[ "$CURRENT_VERSION" == "$LATEST_VERSION" ]]; then
        @info "No updates needed. Versions match."
        exit 0
    fi

    # Update the YAML file with the latest version
    @info "Updating version in $YAML_FILE..."
    yq -i '
      .directories[].contents[]
      |= (select(.path == "justlib").git.ref = "'"$LATEST_VERSION"'")
    ' "$YAML_FILE"

    if [[ $? -eq 0 ]]; then
        @info "The YAML file '$YAML_FILE' has been updated to version $LATEST_VERSION."
        @recipe sync
    else
        @fatal "Failed to update the YAML file."
    fi

# Prints help page, kind of man page for justlib.
[group('global')]
help: (print-markdown source_directory() + "/help.md")
