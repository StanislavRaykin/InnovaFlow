# syntax=docker/dockerfile:1
# ---------------------------------------------------------------------------
# InnovaFlow — one Dockerfile for every service in src/
#
# Render passes each service's environment variables to the build as build args,
# set PROJECT on the Render service and the same file
# builds Gateway, Identity, Projects, Analysis.Api, Analysis.Worker, Notifier, Client.
#
# Local test:
#   docker build --build-arg PROJECT=Gateway -t innovaflow-gateway .
#   docker run -p 8080:8080 -e PORT=8080 innovaflow-gateway




ARG DOTNET_VERSION=10.0

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build

ARG PROJECT
ARG BUILD_CONFIGURATION=Release
RUN test -n "$PROJECT" || (echo "ERROR: set the PROJECT env var" && exit 1)

WORKDIR /repo
COPY . .

# NuGet cache mount speeds up rebuilds (BuildKit)
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore "src/${PROJECT}/${PROJECT}.csproj"

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish "src/${PROJECT}/${PROJECT}.csproj" \
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

USER $APP_UID

EXPOSE 10000
ENTRYPOINT ["/bin/sh", "-c", "export ASPNETCORE_HTTP_PORTS=${PORT:-8080}; exec dotnet \"$APP_DLL\""]