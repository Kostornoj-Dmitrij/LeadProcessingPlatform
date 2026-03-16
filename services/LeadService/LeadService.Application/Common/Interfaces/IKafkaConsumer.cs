namespace LeadService.Application.Common.Interfaces;

/// <summary>
/// Абстракция для потребителя Kafka
/// </summary>
public interface IKafkaConsumer : IDisposable
{
    void Subscribe(IEnumerable<string> topics);

    void Unsubscribe();

    bool IsRunning { get; }
}