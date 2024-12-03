# Base image for the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src
COPY ["Backend/Mod.DynamicEncounters.csproj", "./"]
RUN dotnet restore "Mod.DynamicEncounters.csproj"
COPY Backend/ ./Backend
WORKDIR "/src/Backend"
RUN dotnet build "Mod.DynamicEncounters.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Mod.DynamicEncounters.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Nginx setup stage
FROM nginx:stable-alpine AS nginx

# Create directories for each website
RUN mkdir -p /usr/share/nginx/html/game-ui \
    && mkdir -p /usr/share/nginx/html/party-ui \
    && mkdir -p /usr/share/nginx/html/frontend

# Copy the React builds into their respective directories
COPY game-ui/build/ /usr/share/nginx/html/game-ui/
COPY party-ui/build/ /usr/share/nginx/html/party-ui/
COPY frontend/build/ /usr/share/nginx/html/frontend/

# Copy the custom Nginx configuration file
COPY nginx.conf /etc/nginx/nginx.conf

# Final stage
FROM base AS final
WORKDIR /app

# Copy published .NET app
COPY --from=publish /app/publish .

# Install Nginx in the final image
RUN apk add --no-cache nginx

# Copy Nginx HTML content
COPY --from=nginx /usr/share/nginx/html /usr/share/nginx/html

# Copy specific Nginx configuration files
COPY --from=nginx /etc/nginx/nginx.conf /etc/nginx/nginx.conf
COPY --from=nginx /etc/nginx/mime.types /etc/nginx/mime.types

# Install necessary dependencies for the .NET app
RUN apk add --no-cache \
      gcc \
      g++ \
      make \
      libc-dev \
      libstdc++ \
      gcompat \
      musl-dev

# Expose ports for the websites and the .NET app
EXPOSE 80 8080 9210

# Command to start both the .NET app and Nginx
CMD ["sh", "-c", "dotnet Mod.DynamicEncounters.dll ../config/dual.yaml & nginx -g 'daemon off;'"]
