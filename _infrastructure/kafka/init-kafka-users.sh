#!/bin/bash
set -e

create_user() {
    local username=$1
    local password=$2

    kafka-configs --bootstrap-server localhost:29092 \
      --alter --add-config "SCRAM-SHA-256=[iterations=4096,password=${password}]" \
      --entity-type users --entity-name "${username}"

    echo "Created user: ${username}"
}

create_user "admin" "${KAFKA_ADMIN_PASSWORD}"
create_user "lead-service" "${KAFKA_LEAD_PASSWORD}"
create_user "enrichment-service" "${KAFKA_ENRICH_PASSWORD}"
create_user "scoring-service" "${KAFKA_SCORE_PASSWORD}"
create_user "distribution-service" "${KAFKA_DIST_PASSWORD}"
create_user "notification-service" "${KAFKA_NOTIFY_PASSWORD}"

echo "All SCRAM users created successfully."