#!/bin/bash
set -e

cat > /tmp/admin_client.properties << EOF
security.protocol=SASL_PLAINTEXT
sasl.mechanism=SCRAM-SHA-256
sasl.jaas.config=org.apache.kafka.common.security.scram.ScramLoginModule required username="admin" password="${KAFKA_ADMIN_PASSWORD}";
EOF

echo "Waiting for Kafka to be ready..."
while ! kafka-topics --bootstrap-server kafka:29092 --command-config /tmp/admin_client.properties --list &>/dev/null; do
  echo "Waiting for Kafka SASL..."
  sleep 2
done

echo "Creating topics..."

PREFIX=${TOPIC_PREFIX:-}
SUFFIX=${TOPIC_SUFFIX:-}

create_topic() {
    local base=$1
    local topic="${PREFIX}${base}${SUFFIX}"
    kafka-topics --bootstrap-server kafka:29092 \
      --command-config /tmp/admin_client.properties \
      --create --if-not-exists \
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
kafka-topics --bootstrap-server kafka:29092 --command-config /tmp/admin_client.properties --list