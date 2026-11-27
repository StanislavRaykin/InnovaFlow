# syntax=docker/dockerfile:1
# ---------------------------------------------------------------------------
# InnovaFlow — one Dockerfile for every service in src/
#
# Render passes each service's environment variables to the build as build args,
# so set PROJECT on the Render service (e.g. PROJECT=Gateway) and the same file
# builds Gateway, Identity, Projects, Analysis.Api, Analysis.Worker, Notifier, Client.
#
# Local test:
#   docker build --build-arg PROJECT=Gateway -t innovaflow-gateway .
#   docker run -p 8080:8080 -e PORT=8080 innovaflow-gateway
# ---------------------------------------------------------------------------

ARG DOTNET_VERSION=10.0

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
# ARGs must be declared after FROM for Render's injected values to be visible
ARG PROJECT
ARG BUILD_CONFIGURATION=Release
RUN test -n "$PROJECT" || (echo "ERROR: set the PROJECT env var / build arg (e.g. Gateway)" && exit 1)

WORKDIR /repo
COPY . .

RUN dotnet restore "src/${PROJECT}/${PROJECT}.csproj"

RUN dotnet publish "src/${PROJECT}/${PROJECT}.csproj" \
      -c ${BUILD_CONFIGURATION} \
      -o /app/publish \
      --no-restore \
      /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS final
ARG PROJECT
WORKDIR /app

# FORWARDEDHEADERS: Render terminates TLS and sends X-Forwarded-* headers
ENV APP_DLL=${PROJECT}.dll \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

COPY --from=build --chown=$APP_UID /app/publish .

# Non-root user that ships with the official .NET images
USER $APP_UID

# Render injects PORT (default 10000); fall back to 8080 locally
EXPOSE 10000
ENTRYPOINT ["/bin/sh", "-c", "export ASPNETCORE_HTTP_PORTS=${PORT:-8080}; exec dotnet \"$APP_DLL\""]