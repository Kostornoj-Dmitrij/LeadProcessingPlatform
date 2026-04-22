#!/bin/bash
set -e

echo "Starting temporary Kafka for SCRAM init..."
/etc/confluent/docker/run &

echo "Waiting for Kafka to be ready..."
while ! nc -z localhost 29092; do 
    sleep 2
done

echo "Creating SCRAM users..."
/init-kafka-users.sh

echo "SCRAM users created! Waiting for Zookeeper sync..."
sleep 5

echo "Container will exit now."