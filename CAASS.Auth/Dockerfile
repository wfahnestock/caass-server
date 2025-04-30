FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only what's needed for building
COPY CAASS.Auth/. CAASS.Auth/

# Restore and publish
WORKDIR "/src/CAASS.Auth"
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install PostgreSQL client with verification
RUN apt-get update && \
    apt-get install -y postgresql-client && \
    which psql && \
    psql --version && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy only published files and entrypoint script
COPY --from=build /app/publish .
COPY CAASS.Auth/entrypoint.sh .
RUN chmod 755 /app/entrypoint.sh
RUN ls -la

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DB_HOST=caass_tenants
ENV DB_USER=postgres
ENV DB_PASSWORD=bluesky1!

ENTRYPOINT ["/app/entrypoint.sh"]