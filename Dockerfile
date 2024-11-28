# Use .NET runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Drone2.csproj", "Drone2/"]
RUN dotnet restore "Drone2/Drone2.csproj"
COPY . .
WORKDIR "/src/Drone2"
RUN dotnet build "Drone2.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Drone2.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Drone2.dll"]
