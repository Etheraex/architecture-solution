# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY SecurityService/SecurityService.csproj SecurityService/
RUN dotnet restore SecurityService/SecurityService.csproj

COPY . .
RUN dotnet publish SecurityService/SecurityService.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SecurityService.dll"]