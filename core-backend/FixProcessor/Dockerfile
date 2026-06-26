# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY FixBackendShared/FixBackendShared.csproj FixBackendShared/
COPY FixProcessor/FixProcessor.csproj FixProcessor/
RUN dotnet restore FixProcessor/FixProcessor.csproj

COPY . .
RUN dotnet publish FixProcessor/FixProcessor.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "FixProcessor.dll"]