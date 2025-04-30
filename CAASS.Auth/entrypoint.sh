#!/bin/bash
set -e

# Debug marker to confirm script execution
touch /app/ENTRYPOINT_WAS_EXECUTED
echo "Entrypoint script started at $(date)" > /app/entrypoint_executed.log

# Debug information
echo "$(date): Entrypoint script started" > /app/migration.log
echo "Listing files in /app directory:" | tee -a /app/migration.log
ls -la /app | tee -a /app/migration.log

# Wait for the database
echo "Waiting for database to be ready..." | tee -a /app/migration.log
until PGPASSWORD=$DB_PASSWORD psql -h $DB_HOST -U $DB_USER -d postgres -c '\l' &> /app/db_check.log; do
  echo "Postgres is unavailable - sleeping" | tee -a /app/migration.log
  cat /app/db_check.log >> /app/migration.log
  sleep 5
done
echo "Database is up" | tee -a /app/migration.log

# Check if the DLL exists before trying to run it
if [ ! -f /app/CAASS.Auth.dll ]; then
  echo "ERROR: CAASS.Auth.dll not found in /app directory" | tee -a /app/migration.log
  #exit 1
fi

# Run migrations
echo "Running migrations..." | tee -a /app/migration.log
dotnet /app/CAASS.Auth.dll --migrate | tee -a /app/migration.log

# Start the application
echo "Starting application" | tee -a /app/migration.log
exec dotnet /app/CAASS.Auth.dll