using MayorElection.Domain.Entities;
using MayorElection.Domain.Repositories;

namespace MayorElection.Services;

public enum MoveDirection { Up = -1, Down = 1 }

public sealed class ListOrderService(ICandidateRepository repository, IUnitOfWork unit, UserSession session)
{
    public async Task MoveAsync(Guid id, MoveDirection direction, CancellationToken cancellationToken = default)
    {
        session.RequireUser();
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        await unit.ExecuteInTransactionAsync(async token =>
        {
            var list = (await repository.ListAsync(cancellationToken: token)).Select(x => x.Id).ToList();
            var index = list.IndexOf(id);
            if (index < 0) throw new KeyNotFoundException("Кандидат не найден.");
            var target = index + (int)direction;
            if (target < 0 || target >= list.Count) return;
            (list[index], list[target]) = (list[target], list[index]);
            await ApplyOrderAsync(list, token);
        }, cancellationToken);
    }

    // Caller owns the transaction. Positive temporary positions avoid unique/check constraint collisions.
    internal async Task ApplyOrderAsync(IReadOnlyList<Guid> ids, CancellationToken token)
    {
        var tracked = new List<Candidate>();
        foreach (var id in ids)
            tracked.Add(await repository.GetByIdAsync(id, token) ?? throw new KeyNotFoundException());
        var maximum = tracked.Count == 0 ? 0 : tracked.Max(x => x.Position);
        _ = checked(maximum + tracked.Count);
        for (var index = 0; index < tracked.Count; index++)
            tracked[index].Position = maximum + index + 1;
        await unit.SaveChangesAsync(token);
        for (var index = 0; index < tracked.Count; index++) tracked[index].Position = index + 1;
        await unit.SaveChangesAsync(token);
    }
}
