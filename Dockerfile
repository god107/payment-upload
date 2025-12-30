# syntax=docker/dockerfile:1

# =============================================================================
# Base build stage - shared restore for all projects
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-base
WORKDIR /src

COPY UploadPayments.sln ./
COPY src/ ./src/

RUN dotnet restore ./UploadPayments.sln

# =============================================================================
# API publish stage
# =============================================================================
FROM build-base AS publish-api
RUN dotnet publish ./src/UploadPayments.Api/UploadPayments.Api.csproj -c Release -o /app/publish --no-restore

# =============================================================================
# Worker publish stage
# =============================================================================
FROM build-base AS publish-worker
RUN dotnet publish ./src/UploadPayments.Worker/UploadPayments.Worker.csproj -c Release -o /app/publish --no-restore

# =============================================================================
# Base runtime stage - shared runtime dependencies
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-base
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends libkrb5-3 libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*

# =============================================================================
# API runtime - target: api
# =============================================================================
FROM runtime-base AS api

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

COPY --from=publish-api /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "UploadPayments.Api.dll"]

# =============================================================================
# Worker runtime - target: worker
# Uses lighter runtime image since it's not a web app
# =============================================================================
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS worker
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends libkrb5-3 libgssapi-krb5-2 \
	&& rm -rf /var/lib/apt/lists/*

COPY --from=publish-worker /app/publish ./

ENTRYPOINT ["dotnet", "UploadPayments.Worker.dll"]
