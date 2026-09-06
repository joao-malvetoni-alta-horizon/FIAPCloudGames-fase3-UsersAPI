FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FCG.Domain/FCG.Domain.csproj src/FCG.Domain/
COPY src/FCG.Application/FCG.Application.csproj src/FCG.Application/
COPY src/FCG.Infrastructure/FCG.Infrastructure.csproj src/FCG.Infrastructure/
COPY src/FCG.API/FCG.API.csproj src/FCG.API/

RUN dotnet restore src/FCG.API/FCG.API.csproj

COPY src/ src/

RUN dotnet publish src/FCG.API/FCG.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Agente APM New Relic (Fase 3 - observabilidade).
# O pacote NuGet NewRelic.Agent publica o agente em /app/newrelic. O profiler do
# CoreCLR só é carregado quando estas variáveis estão presentes, então a imagem já
# sobe instrumentada; sem NEW_RELIC_LICENSE_KEY o agente apenas não conecta.
# O GUID do profiler é fixo e case-sensitive.
ENV CORECLR_ENABLE_PROFILING=1 \
    CORECLR_PROFILER={36032161-FFC0-4B61-B559-F6C5D41BAE5A} \
    CORECLR_NEWRELIC_HOME=/app/newrelic \
    CORECLR_PROFILER_PATH=/app/newrelic/libNewRelicProfiler.so \
    NEW_RELIC_APP_NAME=FCG-UsersAPI \
    NEW_RELIC_DISTRIBUTED_TRACING_ENABLED=true \
    NEW_RELIC_APPLICATION_LOGGING_ENABLED=true \
    NEW_RELIC_APPLICATION_LOGGING_FORWARDING_ENABLED=true \
    NEW_RELIC_APPLICATION_LOGGING_LOCAL_DECORATING_ENABLED=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "FCG.API.dll"]
