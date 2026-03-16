using MediatR;
using LeadService.Application.DTOs;
using SharedKernel.Base;

namespace LeadService.Application.Commands.CreateLead;

/// <summary>
/// Команда создания нового лида
/// </summary>
public class CreateLeadCommand : IRequest<LeadDto>, ICommand
{
    /// <summary>
    /// Внешний идентификатор для идемпотентности
    /// </summary>
    public string? ExternalLeadId { get; set; }
    
    /// <summary>
    /// Источник поступления лида
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// Название компании
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Контактное лицо
    /// </summary>
    public string? ContactPerson { get; set; }
    
    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Телефон
    /// </summary>
    public string? Phone { get; set; }
    
    /// <summary>
    /// Дополнительные поля
    /// </summary>
    public Dictionary<string, string>? CustomFields { get; set; }
}