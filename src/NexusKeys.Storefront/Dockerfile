# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 5205
ENV ASPNETCORE_URLS=http://+:5205

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for caching restore layer
COPY ["src/NexusKeys.Storefront/NexusKeys.Storefront.csproj", "src/NexusKeys.Storefront/"]
RUN dotnet restore "src/NexusKeys.Storefront/NexusKeys.Storefront.csproj"

# Copy full source and publish
COPY ["src/NexusKeys.Storefront/", "src/NexusKeys.Storefront/"]
WORKDIR "/src/src/NexusKeys.Storefront"
RUN dotnet publish "NexusKeys.Storefront.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Frontend.dll"]
