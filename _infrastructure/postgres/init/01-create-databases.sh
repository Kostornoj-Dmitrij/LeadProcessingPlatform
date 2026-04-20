#!/bin/bash
set -e

PREFIX=${DB_PREFIX:-}
SUFFIX=${DB_SUFFIX:-}

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    SELECT 'CREATE DATABASE ${PREFIX}lead_db${SUFFIX}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${PREFIX}lead_db${SUFFIX}')\gexec
    SELECT 'CREATE DATABASE ${PREFIX}enrichment_db${SUFFIX}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${PREFIX}enrichment_db${SUFFIX}')\gexec
    SELECT 'CREATE DATABASE ${PREFIX}scoring_db${SUFFIX}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${PREFIX}scoring_db${SUFFIX}')\gexec
    SELECT 'CREATE DATABASE ${PREFIX}distribution_db${SUFFIX}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${PREFIX}distribution_db${SUFFIX}')\gexec
    SELECT 'CREATE DATABASE ${PREFIX}notification_db${SUFFIX}' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '${PREFIX}notification_db${SUFFIX}')\gexec
EOSQL