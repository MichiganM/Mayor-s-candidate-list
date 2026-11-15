using System.ComponentModel.DataAnnotations;

namespace MayorElection.Domain.Enums;

public enum CandidateStatus
{
    [Display(Name = "Зарегистрирован")] Registered = 1,
    [Display(Name = "Снят")] Withdrawn = 2,
    [Display(Name = "Отклонён")] Rejected = 3
}
