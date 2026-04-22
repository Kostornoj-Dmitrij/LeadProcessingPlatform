using Confluent.Kafka;
using SharedHosting.Options;

namespace SharedHosting.Extensions;

/// <summary>
/// Расширения для конфигурации Kafka клиентов
/// </summary>
public static class KafkaConfigExtensions
{
    public static void ApplySaslConfig(this ClientConfig config, KafkaOptions? options)
    {
        if (string.IsNullOrEmpty(options?.Username))
            return;

        config.SecurityProtocol = ParseSecurityProtocol(options.SecurityProtocol);
        config.SaslMechanism = ParseSaslMechanism(options.SaslMechanism);
        config.SaslUsername = options.Username;
        config.SaslPassword = options.Password;
    }

    private static SecurityProtocol ParseSecurityProtocol(string? protocol)
    {
        return protocol?.ToLowerInvariant() switch
        {
            "plaintext" => SecurityProtocol.Plaintext,
            "ssl" => SecurityProtocol.Ssl,
            "saslplaintext" => SecurityProtocol.SaslPlaintext,
            "saslssl" => SecurityProtocol.SaslSsl,
            _ => SecurityProtocol.Plaintext
        };
    }

    private static SaslMechanism ParseSaslMechanism(string? mechanism)
    {
        return mechanism?.ToLowerInvariant() switch
        {
            "gssapi" => SaslMechanism.Gssapi,
            "plain" => SaslMechanism.Plain,
            "scramsha256" => SaslMechanism.ScramSha256,
            "scramsha512" => SaslMechanism.ScramSha512,
            "oauthbearer" => SaslMechanism.OAuthBearer,
            _ => SaslMechanism.ScramSha256
        };
    }
}