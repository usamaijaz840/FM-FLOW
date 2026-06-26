#!/usr/bin/env bash
set -euo pipefail
DB_NAME="${KEYCLOAK_DBNAME:-keycloakdb_test}"
echo "[postgres-init] Ensuring Keycloak DB exists: ${DB_NAME}"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -tAc \
  "SELECT 1 FROM pg_database WHERE datname='${DB_NAME}'" | grep -q 1 \
  || psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" -c "CREATE DATABASE \"${DB_NAME}\""
