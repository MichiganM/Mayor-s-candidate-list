using System.ComponentModel.DataAnnotations;

namespace MayorElection.Services.DTOs;

public sealed record RegisterRequest(
    [property: Required, StringLength(64, MinimumLength = 3)] string UserName,
    [property: Required, StringLength(128, MinimumLength = 12)] string Password,
    [property: Required, Compare("Password")] string ConfirmPassword)
{
    public override string ToString() => "RegisterRequest { credentials redacted }";
}

public enum LoginStatus { Succeeded, InvalidCredentials, RequiresTwoFactor, LockedOut }
public sealed record AuthResult(LoginStatus Status);
public sealed record TwoFactorSetup(string SharedKey, string AuthenticatorUri)
{
    public override string ToString() => "TwoFactorSetup { credentials redacted }";
}
public sealed record SessionUser(string Id, string UserName);
