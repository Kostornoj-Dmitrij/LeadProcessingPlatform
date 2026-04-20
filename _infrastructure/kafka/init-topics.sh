#!/bin/bash
set -e

echo "Waiting for Kafka to be ready..."
cub kafka-ready -b kafka:29092 1 30

echo "Creating topics..."

PREFIX=${TOPIC_PREFIX:-}
SUFFIX=${TOPIC_SUFFIX:-}

create_topic() {
    local base=$1
    local topic="${PREFIX}${base}${SUFFIX}"
    kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
      --topic "$topic" \
      --partitions 3 \
      --replication-factor 1
    echo "Created topic: $topic"
}

create_topic "lead-events"
create_topic "enrichment-events"
create_topic "scoring-events"
create_topic "distribution-events"
create_topic "notification-events"
create_topic "saga-events"

create_topic "lead-service-dlq"
create_topic "enrichment-service-dlq"
create_topic "scoring-service-dlq"
create_topic "distribution-service-dlq"
create_topic "notification-service-dlq"

create_topic "healthcheck"

echo "Topics created:"
kafka-topics --bootstrap-server kafka:29092 --list