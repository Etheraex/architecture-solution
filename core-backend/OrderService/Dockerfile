# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csprojs first for cached restore
COPY OrderService/OrderService.csproj OrderService/
RUN dotnet restore OrderService/OrderService.csproj

COPY . .
RUN dotnet publish OrderService/OrderService.csproj -c Release -o /out

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.dll"]