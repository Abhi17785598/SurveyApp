# Use the official .NET 10.0 runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["SurveyWebApp.csproj", "./"]
RUN dotnet restore "./SurveyWebApp.csproj"

# Copy the rest of the files and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "SurveyWebApp.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SurveyWebApp.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build the final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set environment variables for Render
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Create a simple health check endpoint
RUN echo "#!/bin/sh\ncurl -f http://localhost:8080/health || exit 1" > /app/healthcheck.sh && chmod +x /app/healthcheck.sh
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 CMD /app/healthcheck.sh

ENTRYPOINT ["dotnet", "SurveyWebApp.dll"]
