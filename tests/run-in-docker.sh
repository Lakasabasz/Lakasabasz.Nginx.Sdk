#!/bin/sh
# Runs the tests the way Buddy does: inside the test image (tests/environment), built locally, through ci/run-tests.sh.
# Extra arguments go to dotnet test, e.g. tests/run-in-docker.sh --filter "FullyQualifiedName~NginxServerTests"
set -eu
cd "$(dirname "$0")/.."
image="lakasabasz-nginx-sdk-tests:$(sh ci/test-image-tag.sh)"
docker image inspect "$image" >/dev/null 2>&1 || docker build -t "$image" tests/environment
docker run --rm --init \
    -v "$PWD:/src" -w /src \
    -v lakasabasz-nginx-sdk-nuget:/var/cache/nuget-packages \
    -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    "$image" \
    sh ci/run-tests.sh "$@"
