#!/bin/sh
# Runs the tests and the examples inside the test image: builds as root, then runs them as tester, the unprivileged user of
# the image (see tests/environment/Dockerfile and sudoers). Used by the Tests pipeline and tests/run-in-docker.sh.
# Extra arguments go to dotnet test.
set -eu
cd "$(dirname "$0")/.."
artifacts=/tmp/artifacts
# Every test skipped in the test image, written by SkippedTestsGuard in the test project
skipped=/tmp/nginx-sdk-tests.skipped
# Readable by tester (unlike /root/.nuget): the test run evaluates the projects, which import package files
export NUGET_PACKAGES="${NUGET_PACKAGES:-/var/cache/nuget-packages}"

version=$(sh ci/version.sh)
echo "Building $version"
dotnet build Lakasabasz.Nginx.Sdk.slnx -c Release --artifacts-path "$artifacts" -p:Version="$version"
chown -R tester:tester "$artifacts"
rm -f "$skipped"

status=0
# Results go under the artifacts too: tester cannot write to the sources (owned by root in Buddy)
runuser -u tester -- dotnet test --solution Lakasabasz.Nginx.Sdk.slnx -c Release --artifacts-path "$artifacts" --no-build \
    --results-directory "$artifacts/TestResults" "$@" || status=$?

# The examples (examples/) must keep working: each runs as tester, like an application using the package
for project in examples/*/*.csproj; do
    name=$(basename "$project" .csproj)
    echo "Example $name"
    runuser -u tester -- dotnet "$artifacts/bin/$name/release/$name.dll" || status=1
done

# dotnet test passes with skipped tests; in the test image nothing may be skipped
if [ -s "$skipped" ]; then
    echo "Tests were skipped in the test image:" >&2
    cat "$skipped" >&2
    exit 1
fi
exit "$status"
