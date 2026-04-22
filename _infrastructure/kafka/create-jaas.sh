#!/bin/bash
cat > /tmp/kafka_server_jaas.conf << EOF
KafkaServer {
    org.apache.kafka.common.security.scram.ScramLoginModule required
    username="admin"
    password="${KAFKA_ADMIN_PASSWORD}"
    user_admin="${KAFKA_ADMIN_PASSWORD}"
    user_lead-service="${KAFKA_LEAD_PASSWORD}"
    user_enrichment-service="${KAFKA_ENRICH_PASSWORD}"
    user_scoring-service="${KAFKA_SCORE_PASSWORD}"
    user_distribution-service="${KAFKA_DIST_PASSWORD}"
    user_notification-service="${KAFKA_NOTIFY_PASSWORD}";
};
EOF
echo "JAAS config created."