[private]
just-list file=justfile():
    #!/usr/bin/env bash
    source "{{ source_directory() }}/../libexec/just_helper.bash"
    @info "Available recipes:"
    @hr
    just -f {{ file }} --list-heading '' --list --list-submodules
    @hr
    @info "Use 'just help' or 'just <module-name>::help' to print extended documentation on global recipes and module usage"

[private]
just-choose file=justfile():
    hermit install fzf -q && just -f {{ file }} --choose

[private]
print-markdown filepath *glow-args='':
    #!/usr/bin/env bash

    ## Print help.md
    if [[ -f "{{ filepath }}" ]]; then
      ## Make sure glow is installed
      hermit install glow -q

      cat "{{ filepath }}" | glow {{ glow-args }}
    else
      echo "no such file {{ filepath }}"
    fi

# Allows to disable requires checks entirely.

requires-enabled := env("JUST_REQUIRES_ENABLED", "true")

# Checks if the given programs are available in PATH
[private]
requires *programs:
    #!/usr/bin/env bash
    source "{{ source_directory() }}/../libexec/just_helper.bash"

    if [ "{{ requires-enabled }}" == "false" ]; then
      @debug "require checks are disabled!"
      exit 0
    fi

    if [ -z "{{ programs }}" ]; then
      @fatal "No arguments passed to requires recipe!"
    fi

    missing_programs=()
    for program in {{ programs }}; do
      command -v "${program}" >/dev/null 2>&1 || missing_programs+=("${program}")
    done

    if [ ${#missing_programs[@]} -gt 0 ]; then
      ## Print error messages for all missing programs
      echo -e "${C_BOLD}${C_RED}ERROR:${NO_FORMAT} Missing dependencies:"
      for e in "${missing_programs[@]}"; do
        echo -e "\t${e}"
      done

      @info "Install missing dependencies via hermit"
      @info "hermit install ${missing_programs[@]}"

      exit 1
    fi

# Installs the given packages using hermit
[private]
hermit-packages *packages: (requires "hermit")
    #!/usr/bin/env bash
    hermit install {{ packages }}

# Creates or updates an environment variable in hermit.hcl
[private]
hermit-env name value: (requires "hermit")
    #!/usr/bin/env bash
    hermit env {{ name }} {{ value }}
