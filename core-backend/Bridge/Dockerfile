# syntax=docker/dockerfile:1

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY Shared/Shared.csproj Shared/
COPY Bridge/Bridge.csproj Bridge/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore Bridge/Bridge.csproj

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish Bridge/Bridge.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Bridge.dll"]