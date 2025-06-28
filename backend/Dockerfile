# TP-Link WebUI Backend Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 5000

# Create logs directory
RUN mkdir -p /app/logs

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TPLinkWebUI.csproj", "."]
RUN dotnet restore "TPLinkWebUI.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "TPLinkWebUI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TPLinkWebUI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "TPLinkWebUI.dll"]