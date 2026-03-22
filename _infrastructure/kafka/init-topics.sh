#!/bin/bash
set -e

echo "Waiting for Kafka to be ready..."
cub kafka-ready -b kafka:29092 1 30

echo "Creating topics..."

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic lead-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic enrichment-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic scoring-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic distribution-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic notification-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic saga-events \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic lead-service-dlq \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic enrichment-service-dlq \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic scoring-service-dlq \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic distribution-service-dlq \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic notification-service-dlq \
  --partitions 3 \
  --replication-factor 1

kafka-topics --bootstrap-server kafka:29092 --create --if-not-exists \
  --topic healthcheck \
  --partitions 1 \
  --replication-factor 1
echo "Topics created:"
kafka-topics --bootstrap-server kafka:29092 --list