#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    SELECT 'CREATE DATABASE lead_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'lead_db')\gexec
    SELECT 'CREATE DATABASE enrichment_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'enrichment_db')\gexec
    SELECT 'CREATE DATABASE scoring_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'scoring_db')\gexec
    SELECT 'CREATE DATABASE distribution_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'distribution_db')\gexec
    SELECT 'CREATE DATABASE notification_db' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'notification_db')\gexec
EOSQL