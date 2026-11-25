#!/bin/bash
# Creates least-privilege roles on first database start (local development only):
#   - POSTGRES_USER owns the schema and is used only for migrations,
#   - collector role may read/write data rows (no DDL),
#   - api role may only read.
set -euo pipefail

: "${COLLECTOR_DB_PASSWORD:?COLLECTOR_DB_PASSWORD is required}"
: "${API_DB_PASSWORD:?API_DB_PASSWORD is required}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" \
  -v collector_pw="$COLLECTOR_DB_PASSWORD" -v api_pw="$API_DB_PASSWORD" <<'SQL'
CREATE ROLE bp_collector LOGIN PASSWORD :'collector_pw';
CREATE ROLE bp_api LOGIN PASSWORD :'api_pw';

REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO bp_collector, bp_api;

-- Tables and sequences created later by migrations (owned by POSTGRES_USER) get these grants automatically.
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE ON TABLES TO bp_collector;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO bp_collector;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO bp_api;
SQL
