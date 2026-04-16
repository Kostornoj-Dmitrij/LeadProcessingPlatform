using AvroSchemas.Messages.Base;

namespace SharedInfrastructure.Telemetry;

/// <summary>
/// Extension методы для создания Activity
/// </summary>
public static class ActivityBuilderExtensions
{
    public static ActivityBuilder CreateCommandActivity(string commandName)
        => ActivityBuilder.ForCommand(commandName);

    public static ActivityBuilder CreateEventActivity(IntegrationEventAvro @event, string? customName = null)
        => ActivityBuilder.ForEvent(@event, customName);
}