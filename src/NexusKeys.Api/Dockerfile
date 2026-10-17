# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5021
ENV ASPNETCORE_URLS=http://+:5021

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for caching restore layer
COPY ["src/NexusKeys.Domain/NexusKeys.Domain.csproj", "src/NexusKeys.Domain/"]
COPY ["src/NexusKeys.Infrastructure/NexusKeys.Infrastructure.csproj", "src/NexusKeys.Infrastructure/"]
COPY ["src/NexusKeys.Api/NexusKeys.Api.csproj", "src/NexusKeys.Api/"]
RUN dotnet restore "src/NexusKeys.Api/NexusKeys.Api.csproj"

# Copy full source and publish
COPY ["src/NexusKeys.Domain/", "src/NexusKeys.Domain/"]
COPY ["src/NexusKeys.Infrastructure/", "src/NexusKeys.Infrastructure/"]
COPY ["src/NexusKeys.Api/", "src/NexusKeys.Api/"]
WORKDIR "/src/src/NexusKeys.Api"
RUN dotnet publish "NexusKeys.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GameStore.API.dll"]
