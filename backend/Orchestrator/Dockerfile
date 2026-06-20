# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY FixBackendShared/FixBackendShared.csproj FixBackendShared/
COPY Orchestrator/Orchestrator.csproj Orchestrator/
RUN dotnet restore Orchestrator/Orchestrator.csproj

COPY . .
RUN dotnet publish Orchestrator/Orchestrator.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Orchestrator.dll"]