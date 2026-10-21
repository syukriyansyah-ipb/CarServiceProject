# ============================================================
# STAGE 1: BUILD
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj dulu (layer caching)
COPY ["AutoCareApi.csproj", "./"]
RUN dotnet restore

# Copy semua source
COPY . .

# Build & publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# STAGE 2: RUNTIME
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AutoCareApi.dll"]