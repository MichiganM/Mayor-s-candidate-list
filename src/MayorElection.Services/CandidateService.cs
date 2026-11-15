using FluentValidation;
using MayorElection.Domain.Enums;
using MayorElection.Domain.Repositories;
using MayorElection.Services.DTOs;
using MayorElection.Services.Validation;
using Microsoft.Extensions.Logging;

namespace MayorElection.Services;

public sealed class CandidateService(ICandidateRepository repository, IUnitOfWork unit,
    IValidator<CandidateDto> validator, ListOrderService order, UserSession session, ILogger<CandidateService> logger)
{
    public async Task<IReadOnlyList<CandidateDto>> ListAsync(CandidateStatus? status = null,
        CandidateTemplate? template = null, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new ArgumentOutOfRangeException(nameof(status));
        return (await repository.ListAsync(status, template, cancellationToken)).Select(CandidateMapping.ToDto).ToArray();
    }

    public async Task<CandidateDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        return CandidateMapping.ToDto(await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException("Кандидат не найден."));
    }

    public async Task<CandidateDto> AddAsync(CandidateDto dto, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        await ValidateAsync(dto, cancellationToken);
        CandidateDto? result = null;
        await unit.ExecuteInTransactionAsync(async token =>
        {
            var list = await repository.ListAsync(cancellationToken: token);
            var entity = CandidateMapping.Create(dto with { Id = Guid.NewGuid(), Position = checked(list.Count + 1) });
            await repository.AddAsync(entity, token);
            await unit.SaveChangesAsync(token);
            result = CandidateMapping.ToDto(entity);
        }, cancellationToken);
        logger.LogInformation("Candidate {CandidateId} created", result!.Id);
        return result;
    }

    public async Task UpdateAsync(Guid id, CandidateDto dto, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        await ValidateAsync(dto, cancellationToken);
        await unit.ExecuteInTransactionAsync(async token =>
        {
            var entity = await repository.GetByIdAsync(id, token) ?? throw new KeyNotFoundException("Кандидат не найден.");
            if (entity.Template != dto.Template)
                throw new InvalidOperationException("Шаблон существующего кандидата менять нельзя. Создайте новую запись.");
            CandidateMapping.Apply(dto, entity);
            await unit.SaveChangesAsync(token);
        }, cancellationToken);
        logger.LogInformation("Candidate {CandidateId} updated", id);
    }

    public async Task ChangeStatusAsync(Guid id, CandidateStatus status, CancellationToken cancellationToken = default)
    {
        var dto = await GetAsync(id, cancellationToken);
        await UpdateAsync(id, dto with { Status = status }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        await unit.ExecuteInTransactionAsync(async token =>
        {
            var entity = await repository.GetByIdAsync(id, token) ?? throw new KeyNotFoundException("Кандидат не найден.");
            repository.Remove(entity);
            await unit.SaveChangesAsync(token);
            var remaining = await repository.ListAsync(cancellationToken: token);
            await order.ApplyOrderAsync(remaining.Select(x => x.Id).ToArray(), token);
        }, cancellationToken);
        logger.LogInformation("Candidate {CandidateId} deleted", id);
    }

    internal async Task ReplaceAsync(IReadOnlyList<CandidateDto> candidates, CancellationToken cancellationToken)
    {
        session.RequireUser();
        foreach (var candidate in candidates) await ValidateAsync(candidate, cancellationToken);
        if (candidates.Any(x => x.Id == Guid.Empty) || candidates.Select(x => x.Id).Distinct().Count() != candidates.Count
            || !candidates.Select(x => x.Position).Order().SequenceEqual(Enumerable.Range(1, candidates.Count)))
            throw new InvalidDataException("Идентификаторы должны быть уникальны, номера — от 1 до количества кандидатов.");
        await unit.ExecuteInTransactionAsync(async token =>
        {
            foreach (var existing in await repository.ListAsync(cancellationToken: token))
                repository.Remove(await repository.GetByIdAsync(existing.Id, token) ?? throw new KeyNotFoundException());
            await unit.SaveChangesAsync(token);
            foreach (var candidate in candidates.OrderBy(x => x.Position))
                await repository.AddAsync(CandidateMapping.Create(candidate), token);
            await unit.SaveChangesAsync(token);
        }, cancellationToken);
        logger.LogInformation("Candidate list imported: {Count} entries", candidates.Count);
    }

    private async Task ValidateAsync(CandidateDto dto, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(dto);
        DtoValidation.Validate(dto);
        await validator.ValidateAndThrowAsync(dto, token);
    }
}
