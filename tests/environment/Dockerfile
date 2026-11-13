# Test environment for Lakasabasz.Nginx.Sdk: .NET SDK + nginx from Ubuntu packages + config preset.
# Tests run inside this image on its nginx (/etc/nginx); NginxRegistry in the test project shares it between
# test classes and restores the preset after every change
# (tests/Lakasabasz.Nginx.Sdk.Tests/Infrastructure/NginxRegistry.cs).
#
# The image tag is a hash of this directory (ci/test-image-tag.sh), so any change here builds a new image.

# .NET 8 has no Ubuntu resolute image; its runtime is taken from noble
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble AS runtime8
FROM mcr.microsoft.com/dotnet/runtime:10.0-resolute AS runtime10

FROM mcr.microsoft.com/dotnet/sdk:11.0-resolute

# net8.0 and net10.0 test targets next to the SDK's own 11.0 runtime
COPY --from=runtime8 /usr/share/dotnet/shared/Microsoft.NETCore.App/ /usr/share/dotnet/shared/Microsoft.NETCore.App/
COPY --from=runtime10 /usr/share/dotnet/shared/Microsoft.NETCore.App/ /usr/share/dotnet/shared/Microsoft.NETCore.App/

# Pinned to nginx 1.28.3 (Ubuntu resolute); only the Ubuntu patch revision may float
RUN apt-get update \
    && apt-get install -y --no-install-recommends 'nginx=1.28.3-2ubuntu1*' sudo \
    && rm -rf /var/lib/apt/lists/*

COPY preset/ /etc/nginx/
RUN ln -s /etc/nginx/sites-available/reports /etc/nginx/sites-enabled/reports \
    && nginx -t

# The tests run as tester (ci/run-tests.sh): it owns the config, and runs nginx only through sudo (sudoers),
# also from a path with spaces
RUN useradd --create-home tester \
    && chown -R tester:tester /etc/nginx \
    && mkdir -p "/opt/nginx with spaces" \
    && ln -s /usr/sbin/nginx "/opt/nginx with spaces/nginx"
COPY --chmod=440 sudoers /etc/sudoers.d/nginx-sdk-tests
RUN visudo -c

# Corpus for the parser and serializer: the config files of the official nginx image (the Ubuntu ones are in
# /etc/nginx); the tests load and serialize each file and expect it unchanged
COPY --from=nginx:1.28.3 /etc/nginx /opt/nginx-corpus/official

# Marks the test image, for tests that only make sense in it (e.g. sudo rules)
ENV NGINX_SDK_TEST_IMAGE=1
