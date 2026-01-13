# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY src/api/HoHemaLoans.Api/*.csproj ./
RUN dotnet restore

# Copy source code and build
COPY src/api/HoHemaLoans.Api/ ./
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Install SQLite and curl (Alpine uses apk)
RUN apk add --no-cache sqlite curl

# Create data directory for SQLite database
RUN mkdir -p /app/data

# Create non-root user
RUN adduser --disabled-password --gecos "" --uid 1001 appuser
RUN chown -R appuser:appuser /app

COPY --from=publish /app/publish .

# Use non-root user
USER appuser

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/api/health || exit 1

ENTRYPOINT ["dotnet", "HoHemaLoans.Api.dll"]