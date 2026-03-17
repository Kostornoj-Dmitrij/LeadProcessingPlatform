namespace LeadService.Domain.Enums;

/// <summary>
/// Статусы лида
/// </summary>
public enum LeadStatus
{
    /// <summary>
    /// Начальное состояние, лид только что создан
    /// </summary>
    Initial = 1,
    
    /// <summary>
    /// Успешно прошел валидацию (обогащен и оценен)
    /// </summary>
    Qualified = 2,
    
    /// <summary>
    /// Отклонен (ошибка обогащения или скоринга)
    /// </summary>
    Rejected = 3,
    
    /// <summary>
    /// Ошибка на этапе распределения
    /// </summary>
    FailedDistribution = 4,
    
    /// <summary>
    /// Успешно распределен
    /// </summary>
    Distributed = 5,
    
    /// <summary>
    /// Финальное состояние (закрыт)
    /// </summary>
    Closed = 6
}