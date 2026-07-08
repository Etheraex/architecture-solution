# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY RestApiService/RestApiService.csproj RestApiService/
COPY FixBackendShared/FixBackendShared.csproj FixBackendShared/
RUN dotnet restore RestApiService/RestApiService.csproj

COPY . .
RUN dotnet publish RestApiService/RestApiService.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RestApiService.dll"]