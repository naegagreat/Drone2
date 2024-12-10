# Use .NET runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Drone2.csproj", "./"]
RUN dotnet restore "Drone2.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "Drone2.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
WORKDIR /src
RUN dotnet publish "Drone2.csproj" -c Release -o /app/publish

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Add Volume for Data Protection Keys
RUN mkdir /keys
VOLUME /keys

ENTRYPOINT ["dotnet", "Drone2.dll"]
