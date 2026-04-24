# This file is meant to be imported by vendir hooks (.vendir.just) files.

import 'partial/settings.just'
import 'partial/vars.just'
import 'partial/utils.just'

# Get the relative path between invokation and justfile directroy to create symlinks

justlib-dir := source_directory()
hook-directory := justfile_directory()
project-directory := shell("pwd")
hook-relative-path := shell("perl -le 'use File::Spec; print File::Spec->abs2rel(@ARGV)' $1 $2", hook-directory, project-directory)

# Creates a symlink from given source to vendir/.links folder. This is useful to avoid long or nested includes.
[private]
link-folder link-source:
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    if [ ! -d "{{ join(hook-directory, link-source) }}" ]; then
      @fatal "Invalid Argument: '{{ join(hook-directory, link-source) }}' no such directory"
    fi

    # Create a directory for linking folders
    mkdir -p "{{ project-directory }}/vendir/.links"

    @info "Linking folder {{ join(hook-relative-path, link-source) }} to vendir/.links/{{ link-source }}"
    ln -sf "../../{{ join(hook-relative-path, link-source) }}" "vendir/.links/$(basename {{ link-source }})"

[private]
link-executable executable-source executable-name='':
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    if [ ! -f "{{ join(hook-directory, executable-source) }}" ]; then
      @fatal "Invalid Argument: '{{ join(hook-directory, executable-source) }}' no such directory"
    fi

    # Create a directory for linking folders
    mkdir -p "{{ project-directory }}/bin"

    # Make file executable
    chmod +x "{{ join(hook-directory, executable-source) }}"

    LINK_NAME="{{ if executable-name != "" { executable-name } else { shell("basename $1", executable-source) } }}"

    @info "Linking executable {{ join(hook-directory, executable-source) }}"
    ln -sf "../{{ join(hook-relative-path, executable-source) }}" "bin/${LINK_NAME}"

[private]
link-file link-source link-destination='':
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    # Check if the link-source exists
    if [ ! -f "{{ join(hook-directory, link-source) }}" ]; then
      @fatal "Invalid Argument: '{{ join(hook-directory, link-source) }}' no such directory"
    fi

    # ---
    # SOURCE
    # -
    # Source file relative to justfile_directory() which is the path to the justfile executing the hook calling this recipe
    # This is the location the symlink will point to
    SOURCE="{{ clean(link-source) }}"
    SOURCE_ABS="{{ join(hook-directory, clean(link-source)) }}"
    SOURCE_DIR="$(dirname $SOURCE_ABS)"

    # ---
    # DESTINATION
    # -
    # Destination relative to invocation_directory() which is the project root
    # This is the location the symlink will be created in
    DESTINATION="{{ clean(if link-destination != "" { link-destination } else { link-source }) }}"
    DESTINATION_ABS="{{ join(project-directory, clean(if link-destination != "" { link-destination } else { link-source })) }}"
    DESTINATION_DIR="$(dirname $DESTINATION_ABS)"

    ## Do not overwrite existing files if they are not a symlink
    if [ -f "${DESTINATION_ABS}" ] && [ ! -L "${DESTINATION_ABS}" ]; then
      @warn "Skipping symlink creation for '${DESTINATION}' as it seems to be managed manually.\nPlease delete the file to properly link it"
      exit 0
    fi

    # to use relative symlinks, we need to get the relative path from our symlink location to the directory it is supposed to reference, RELATIVE_PATH is exactly that
    RELATIVE_PATH=$( perl -le 'use File::Spec; print File::Spec->abs2rel(@ARGV)' $( echo $SOURCE_DIR $DESTINATION_DIR ))

    # if the destination contains a path to a directory, create the directory
    if [[ "${DESTINATION_DIR}" == *\/* ]]; then
      mkdir -p $DESTINATION_DIR
    fi

    @info "Linking file from ${RELATIVE_PATH}/$(basename $SOURCE) to ${DESTINATION}"

    # create the symlink
    ln -sf "${RELATIVE_PATH}/$(basename $SOURCE)" "${DESTINATION}"

[private]
__copy-file file-source file-destination='' *cp-args='':
    #!/usr/bin/env bash
    source "{{ source_directory() }}/libexec/just_helper.bash"

    # Check if the file-source exists
    if [ ! -f "{{ join(hook-directory, file-source) }}" ]; then
      @fatal "Invalid Argument: '{{ join(hook-directory, file-source) }}' no such directory"
    fi

    # ---
    # SOURCE
    # -
    # Source file relative to justfile_directory() which is the path to the justfile executing the hook calling this recipe
    # This is the location the file will be copied from
    SOURCE="{{ clean(file-source) }}"
    SOURCE_ABS="{{ join(hook-directory, clean(file-source)) }}"
    SOURCE_DIR="$(dirname $SOURCE_ABS)"

    # ---
    # DESTINATION
    # -
    # Destination relative to invocation_directory() which is the project root
    # This is the location the file will be copied to
    DESTINATION="{{ clean(if file-destination != "" { file-destination } else { file-source }) }}"
    DESTINATION_ABS="{{ join(project-directory, clean(if file-destination != "" { file-destination } else { file-source })) }}"
    DESTINATION_DIR="$(dirname $DESTINATION_ABS)"

    ## Do not overwrite existing files if they are not a symlink
    if [ -f "${DESTINATION_ABS}" ] && [ ! -L "${DESTINATION_ABS}" ] && [[ "{{ cp-args }}" != *"-f"* ]]; then
      @warn "Skipping copying for '${DESTINATION}' as it seems to be managed manually.\nPlease delete the file to update it"
      exit 0
    fi

    # Do not copy interactive in CI
    if [ -n "${CI}" ] && [[ "{{ cp-args }}" == *"-i"* ]]; then
      @debug "Skipping interactive copy in CI"
      exit 0
    fi

    @info "Copying file from ${SOURCE}" to "${DESTINATION}"

    # if the destination contains a path to a directory, create the directory
    if [[ "${DESTINATION_DIR}" == *\/* ]]; then
      mkdir -p $DESTINATION_DIR
    fi

    cp {{ cp-args }} "${SOURCE_ABS}" "${DESTINATION_ABS}"

[private]
copy-file file-source file-destination='': (__copy-file file-source file-destination)

[private]
copy-overwrite-file file-source file-destination='': (__copy-file file-source file-destination '-f')

[private]
copy-interactive-file file-source file-destination='': (__copy-file file-source file-destination '-i')
