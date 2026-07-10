# syntax=docker/dockerfile:1

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY Shared/Shared.csproj Shared/
COPY Shared.Web/Shared.Web.csproj Shared.Web/
COPY ConfigurationService/ConfigurationService.csproj ConfigurationService/
COPY TradeData/TradeData.csproj TradeData/
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore ConfigurationService/ConfigurationService.csproj

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish ConfigurationService/ConfigurationService.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ConfigurationService.dll"]