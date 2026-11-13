#!/bin/sh
# Prints the tag of the test image: a hash of everything in tests/environment.
# Used by both Buddy pipelines (test-image builds it, tests runs in it) and by tests/run-in-docker.sh.
set -eu
cd "$(dirname "$0")/.."
hash=$(find tests/environment -type f -print0 | LC_ALL=C sort -z | xargs -0 sha256sum | sha256sum | cut -c1-16)
echo "env-$hash"
