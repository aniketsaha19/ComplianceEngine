# Build context is the repository root (engine/Dockerfile), matching what the
# Container Apps / GitHub Actions build uses.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so dependency layers cache independently of source changes.
# global.json is deliberately not copied: it pins an exact local SDK patch that
# the build image need not match.
COPY NuGet.config ./
COPY engine/ComplianceEngine.csproj engine/
RUN dotnet restore engine/ComplianceEngine.csproj

COPY engine/ engine/
RUN dotnet publish engine/ComplianceEngine.csproj -c Release -o /app --no-restore

# Run
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# The ingress terminates TLS and forwards plain HTTP to this port.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Non-root user shipped with the runtime image; the app writes nothing to disk.
USER $APP_UID

ENTRYPOINT ["dotnet", "ComplianceEngine.dll"]
