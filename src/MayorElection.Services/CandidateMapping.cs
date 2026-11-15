using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using MayorElection.Services.DTOs;

namespace MayorElection.Services;

internal static class CandidateMapping
{
    public static CandidateDto ToDto(Candidate entity) => new()
    {
        Id = entity.Id, FirstName = entity.FirstName, LastName = entity.LastName,
        Patronymic = entity.Patronymic, BirthDate = entity.BirthDate,
        RegistrationDate = entity.RegistrationDate, Status = entity.Status,
        Template = entity.Template, Position = entity.Position,
        PartyName = (entity as PartyCandidate)?.PartyName
    };

    public static Candidate Create(CandidateDto dto)
    {
        Candidate entity = dto.Template switch
        {
            CandidateTemplate.SelfNominee => new SelfNominee(),
            CandidateTemplate.PartyCandidate => new PartyCandidate(),
            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };
        entity.Id = dto.Id == Guid.Empty ? Guid.NewGuid() : dto.Id;
        Apply(dto, entity);
        entity.Position = dto.Position;
        return entity;
    }

    public static void Apply(CandidateDto dto, Candidate entity)
    {
        entity.FirstName = dto.FirstName.Trim(); entity.LastName = dto.LastName.Trim();
        entity.Patronymic = string.IsNullOrWhiteSpace(dto.Patronymic) ? null : dto.Patronymic.Trim();
        entity.BirthDate = dto.BirthDate; entity.RegistrationDate = dto.RegistrationDate;
        entity.Status = dto.Status;
        if (entity is PartyCandidate party) party.PartyName = dto.PartyName!.Trim();
    }
}
