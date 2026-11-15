using MayorElection.Services.DTOs;

namespace MayorElection.Services;

public sealed class UserSession
{
    public SessionUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser is not null;
    internal void SignIn(SessionUser user) => CurrentUser = user;
    internal void Clear() => CurrentUser = null;
    public SessionUser RequireUser() => CurrentUser
        ?? throw new UnauthorizedAccessException("Требуется вход в систему.");
}
