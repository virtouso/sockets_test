# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY BlazorClient.csproj .
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime stage - use nginx to serve the static files
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Copy all published files (Blazor WebAssembly outputs everything needed)
COPY --from=build /app/publish/wwwroot .

# Copy nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# nginx runs automatically, no CMD needed