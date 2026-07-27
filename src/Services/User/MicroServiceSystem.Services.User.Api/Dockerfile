FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/User/MicroServiceSystem.Services.User.Api/MicroServiceSystem.Services.User.Api.csproj
RUN dotnet publish src/Services/User/MicroServiceSystem.Services.User.Api/MicroServiceSystem.Services.User.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MicroServiceSystem.Services.User.Api.dll"]
