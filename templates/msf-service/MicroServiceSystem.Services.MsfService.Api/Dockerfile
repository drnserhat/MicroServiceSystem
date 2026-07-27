FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /source
COPY Directory.Build.props Directory.Packages.props global.json ./
COPY src/ src/
RUN dotnet restore "src/Services/MsfService/MicroServiceSystem.Services.MsfService.Api/MicroServiceSystem.Services.MsfService.Api.csproj"
RUN dotnet publish "src/Services/MsfService/MicroServiceSystem.Services.MsfService.Api/MicroServiceSystem.Services.MsfService.Api.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
USER appuser
ENTRYPOINT ["dotnet", "MicroServiceSystem.Services.MsfService.Api.dll"]
