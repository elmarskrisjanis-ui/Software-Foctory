# syntax=docker/dockerfile:1
# Builds either the API or the collector: docker build --target api|collector .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props BezProcentu.slnx ./
COPY src/BezProcentu.Core/BezProcentu.Core.csproj src/BezProcentu.Core/packages.lock.json src/BezProcentu.Core/
COPY src/BezProcentu.Data/BezProcentu.Data.csproj src/BezProcentu.Data/packages.lock.json src/BezProcentu.Data/
COPY src/BezProcentu.Api/BezProcentu.Api.csproj src/BezProcentu.Api/packages.lock.json src/BezProcentu.Api/
COPY src/BezProcentu.Collector/BezProcentu.Collector.csproj src/BezProcentu.Collector/packages.lock.json src/BezProcentu.Collector/
# Optional build secret "extra_ca": an additional trusted CA (e.g. a corporate TLS-inspecting proxy).
# It is only mounted for this step and never stored in an image layer. TLS verification stays on.
RUN --mount=type=secret,id=extra_ca,required=false \
    if [ -s /run/secrets/extra_ca ]; then \
      cat /etc/ssl/certs/ca-certificates.crt /run/secrets/extra_ca > /tmp/ca.pem && export SSL_CERT_FILE=/tmp/ca.pem; \
    fi \
 && dotnet restore src/BezProcentu.Api/BezProcentu.Api.csproj --locked-mode \
 && dotnet restore src/BezProcentu.Collector/BezProcentu.Collector.csproj --locked-mode \
 ; status=$?; rm -f /tmp/ca.pem; exit $status
COPY src/ src/
RUN dotnet publish src/BezProcentu.Api/BezProcentu.Api.csproj -c Release -o /out/api --no-restore \
 && dotnet publish src/BezProcentu.Collector/BezProcentu.Collector.csproj -c Release -o /out/collector --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api ./
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "BezProcentu.Api.dll"]

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS collector
WORKDIR /app
COPY --from=build /out/collector ./
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
USER app
ENTRYPOINT ["dotnet", "BezProcentu.Collector.dll"]
