using FluentValidation;

namespace LeadService.Application.Commands.CreateLead;

/// <summary>
/// Валидатор команды создания лида
/// </summary>
public class CreateLeadCommandValidator : AbstractValidator<CreateLeadCommand>
{
    public CreateLeadCommandValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required")
            .MaximumLength(100).WithMessage("Source must not exceed 100 characters");
        
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(255).WithMessage("Company name must not exceed 255 characters");
        
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");
        
        RuleFor(x => x.Phone)
            .MaximumLength(50).WithMessage("Phone must not exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.Phone));
        
        RuleFor(x => x.ExternalLeadId)
            .MaximumLength(255).WithMessage("ExternalLeadId must not exceed 255 characters")
            .When(x => !string.IsNullOrEmpty(x.ExternalLeadId));
    }
}