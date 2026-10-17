# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 3001
ENV ASPNETCORE_URLS=http://+:3001

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files for caching restore layer
COPY ["src/NexusKeys.Admin/NexusKeys.Admin.csproj", "src/NexusKeys.Admin/"]
RUN dotnet restore "src/NexusKeys.Admin/NexusKeys.Admin.csproj"

# Copy full source and publish
COPY ["src/NexusKeys.Admin/", "src/NexusKeys.Admin/"]
WORKDIR "/src/src/NexusKeys.Admin"
RUN dotnet publish "NexusKeys.Admin.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Backend.dll"]
