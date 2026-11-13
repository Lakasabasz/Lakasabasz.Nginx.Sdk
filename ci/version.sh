#!/bin/sh
# Prints the package version of this build, passed to dotnet build as -p:Version (ci/run-tests.sh):
#   master:         <base>.<run id>, e.g. 0.3.0.57
#   other branches: <base>-<branch>.<run id>, never mistaken for a release
#   outside Buddy:  <base>-local
# <base> is the newest '## MAJOR.MINOR.PATCH' section of CHANGELOG.md, 0.0.0 until the first release.
set -eu
cd "$(dirname "$0")/.."
base=$(sed -n 's/^## \([0-9]*\.[0-9]*\.[0-9]*\)[[:space:]]*$/\1/p' CHANGELOG.md 2>/dev/null | head -n 1)
base=${base:-0.0.0}

if [ -z "${BUDDY_RUN_ID:-}" ]; then
    echo "$base-local"
elif [ "${BUDDY_RUN_BRANCH:-}" = master ]; then
    echo "$base.$BUDDY_RUN_ID"
else
    branch=$(printf '%s' "${BUDDY_RUN_BRANCH:-}" | tr -c 'A-Za-z0-9' '-')
    echo "$base-$branch.$BUDDY_RUN_ID"
fi
