using System.ComponentModel.DataAnnotations;

namespace MayorElection.Services.Validation;

internal static class DtoValidation
{
    public static void Validate(object dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
            throw new FluentValidation.ValidationException(results.Select(x =>
                new FluentValidation.Results.ValidationFailure(
                    x.MemberNames.FirstOrDefault() ?? "", x.ErrorMessage ?? "Некорректное значение.")));
    }
}
