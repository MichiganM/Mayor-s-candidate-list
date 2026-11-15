using FluentValidation;
using MayorElection.Domain.Enums;
using MayorElection.Services.DTOs;

namespace MayorElection.Services.Validation;

public sealed class CandidateValidator : AbstractValidator<CandidateDto>
{
    public CandidateValidator(TimeProvider clock)
    {
        const string name = @"^[\p{L}\p{M}]+(?:[ '\-’][\p{L}\p{M}]+)*$";
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100).Matches(name);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100).Matches(name);
        RuleFor(x => x.Patronymic).MaximumLength(100).Matches(name)
            .When(x => !string.IsNullOrEmpty(x.Patronymic));
        RuleFor(x => x.BirthDate).Must(date => date > DateOnly.MinValue && date <= Today())
            .WithMessage("Некорректная дата рождения.");
        RuleFor(x => x.RegistrationDate).Must(date => date > DateOnly.MinValue && date <= Today())
            .WithMessage("Некорректная дата регистрации.")
            .GreaterThanOrEqualTo(x => x.BirthDate);
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Template).IsInEnum();
        RuleFor(x => x.PartyName).NotEmpty().MaximumLength(200)
            .When(x => x.Template == CandidateTemplate.PartyCandidate);
        RuleFor(x => x.PartyName).Empty().When(x => x.Template == CandidateTemplate.SelfNominee);
        DateOnly Today() => DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
    }
}
