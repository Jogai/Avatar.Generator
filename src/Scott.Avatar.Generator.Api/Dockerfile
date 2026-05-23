# syntax=docker/dockerfile:1
# Multi-stage Native AOT build for the avatar generator minimal API.
# Native AOT compiles to a small, self-contained native binary — fast cold start and
# no .NET runtime in the final image. Build context must be the repository root (the
# personas/ asset set lives there, outside src/, and is embedded into the core library):
#   podman build -f src/Scott.Avatar.Generator.Api/Dockerfile -t avatar-api .
#   podman run --rm -p 8080:8080 avatar-api

# The -dev variant ships apk + a shell so we can add the Native AOT toolchain.
FROM cgr.dev/chainguard/dotnet-sdk:latest-dev AS build
USER root
# Native AOT links the image with a C toolchain; clang, a linker (lld) and zlib headers.
RUN apk add --no-cache build-base clang lld zlib-dev
WORKDIR /repo

# Native AOT cannot cross-compile between CPU architectures, so target the build
# container's own arch. Detect it from the running container (uname), which is reliable
# regardless of whether the builder populates TARGETARCH.
RUN case "$(uname -m)" in \
      aarch64|arm64) echo linux-arm64 > /tmp/rid ;; \
      x86_64|amd64)  echo linux-x64   > /tmp/rid ;; \
      *) echo "Unsupported build architecture: $(uname -m)" >&2; exit 1 ;; \
    esac

# Warm the NuGet cache (csproj-only layer) so source-only edits don't re-download.
COPY src/Scott.Avatar.Generator/Scott.Avatar.Generator.csproj src/Scott.Avatar.Generator/
COPY src/Scott.Avatar.Generator.Api/Scott.Avatar.Generator.Api.csproj src/Scott.Avatar.Generator.Api/
RUN dotnet restore -r "$(cat /tmp/rid)" src/Scott.Avatar.Generator.Api/Scott.Avatar.Generator.Api.csproj

# Build & publish the native image. We let publish run its own restore (no --no-restore):
# the Native AOT compiler packages are only staged during a publish-context restore, so
# --no-restore would leave ILC without its PrivateSdkAssemblies. The personas/ set is
# embedded by the core library.
COPY src/Scott.Avatar.Generator/ src/Scott.Avatar.Generator/
COPY src/Scott.Avatar.Generator.Api/ src/Scott.Avatar.Generator.Api/
COPY personas/ personas/
RUN dotnet publish src/Scott.Avatar.Generator.Api/Scott.Avatar.Generator.Api.csproj \
    -c Release -r "$(cat /tmp/rid)" -o /app

# The output is a native executable, so the runtime image only needs glibc — no .NET.
FROM cgr.dev/chainguard/glibc-dynamic:latest AS runtime
WORKDIR /app
COPY --from=build /app/Scott.Avatar.Generator.Api ./avatar-api

# Runs as the non-root user shipped in the chainguard image.
USER nonroot
ENV ASPNETCORE_ENVIRONMENT=Production \
	ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["./avatar-api"]
