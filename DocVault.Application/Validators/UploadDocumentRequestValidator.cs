using DocVault.Application.DTOs.Documents;
using FluentValidation;

namespace DocVault.Application.Validators;

public class UploadDocumentRequestValidator : AbstractValidator<UploadDocumentRequest>
{
    public UploadDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description != null);
        RuleFor(x => x.DepartmentId).GreaterThan(0);
        RuleFor(x => x.FinancialYearId).GreaterThan(0);
        RuleFor(x => x.DocumentTypeId).GreaterThan(0);
    }
}
