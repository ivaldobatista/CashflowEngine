# base
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Cashflow.Launch/Cashflow.Launch.API/Cashflow.Launch.API.csproj", "Cashflow.Launch/Cashflow.Launch.API/"]
RUN dotnet restore "Cashflow.Launch/Cashflow.Launch.API/Cashflow.Launch.API.csproj"
COPY . .
WORKDIR "/src/Cashflow.Launch/Cashflow.Launch.API"
RUN dotnet build "Cashflow.Launch.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Cashflow.Launch.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cashflow.Launch.API.dll"]
