set -eu

APP_DATABASE="${SUPPY_APP_DATABASE:-suppy_inventory_update}"
KEYCLOAK_DATABASE="${SUPPY_KEYCLOAK_DATABASE:-suppy_inventory_keycloak}"

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
SELECT 'CREATE DATABASE ' || quote_ident('$APP_DATABASE')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = '$APP_DATABASE'
)\gexec

SELECT 'CREATE DATABASE ' || quote_ident('$KEYCLOAK_DATABASE')
WHERE NOT EXISTS (
    SELECT 1 FROM pg_database WHERE datname = '$KEYCLOAK_DATABASE'
)\gexec
EOSQL
