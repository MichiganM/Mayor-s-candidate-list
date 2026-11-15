using System.Text.RegularExpressions;
using MayorElection.Domain.Entities;
using MayorElection.Services.DTOs;
using MayorElection.Services.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MayorElection.Services;

/// <summary>Scoped desktop sign-in flow. A password challenge stays in this scope for five minutes.</summary>
public sealed class AuthService(UserManager<ApplicationUser> users, UserSession session,
    TimeProvider clock, ILogger<AuthService> logger)
{
    private string? pendingUserId;
    private DateTimeOffset challengeExpires;

    public async Task RegisterAsync(RegisterRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        DtoValidation.Validate(request);
        if (!Regex.IsMatch(request.UserName, @"^[a-zA-Z0-9_.-]{3,64}$"))
            throw new ArgumentException("Логин: 3–64 латинских буквы, цифры, _, . или -.");
        var user = new ApplicationUser { UserName = request.UserName, LockoutEnabled = true };
        EnsureSuccess(await users.CreateAsync(user, request.Password));
        logger.LogInformation("User {UserId} registered", user.Id);
    }

    public async Task<AuthResult> LoginAsync(string userName, string password)
    {
        Logout();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password) || password.Length > 128)
            return new(LoginStatus.InvalidCredentials);
        var user = await users.FindByNameAsync(userName.Trim());
        if (user is null) return new(LoginStatus.InvalidCredentials);
        if (await users.IsLockedOutAsync(user)) return new(LoginStatus.LockedOut);
        if (!await users.CheckPasswordAsync(user, password)) return await FailedAsync(user);
        if (await users.GetTwoFactorEnabledAsync(user))
        {
            pendingUserId = user.Id;
            challengeExpires = clock.GetUtcNow().AddMinutes(5);
            // Do not reset failed attempts until the second factor has succeeded.
            return new(LoginStatus.RequiresTwoFactor);
        }
        return await CompleteLoginAsync(user);
    }

    public async Task<AuthResult> Verify2FAAsync(string code)
    {
        if (pendingUserId is null || clock.GetUtcNow() >= challengeExpires)
        {
            pendingUserId = null;
            return new(LoginStatus.InvalidCredentials);
        }
        var user = await users.FindByIdAsync(pendingUserId);
        if (user is null) { pendingUserId = null; return new(LoginStatus.InvalidCredentials); }
        if (await users.IsLockedOutAsync(user)) { pendingUserId = null; return new(LoginStatus.LockedOut); }
        if (!await users.GetTwoFactorEnabledAsync(user) || !await VerifyCodeAsync(user, code))
        {
            var result = await FailedAsync(user);
            if (result.Status == LoginStatus.LockedOut) pendingUserId = null;
            return result;
        }
        return await CompleteLoginAsync(user);
    }

    public async Task<TwoFactorSetup> Begin2FASetupAsync(string currentPassword)
    {
        var user = await CurrentUserAsync();
        if (await users.IsLockedOutAsync(user)) { Logout(); throw new UnauthorizedAccessException("Учётная запись заблокирована."); }
        if (!await users.CheckPasswordAsync(user, currentPassword))
        {
            if ((await FailedAsync(user)).Status == LoginStatus.LockedOut) Logout();
            throw new UnauthorizedAccessException("Неверный текущий пароль.");
        }
        if (await users.GetTwoFactorEnabledAsync(user))
            throw new InvalidOperationException("2FA уже включена.");
        var key = await users.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            EnsureSuccess(await users.ResetAuthenticatorKeyAsync(user));
            key = await users.GetAuthenticatorKeyAsync(user)
                ?? throw new InvalidOperationException("Не удалось создать ключ аутентификатора.");
        }
        const string issuer = "MayorElection";
        var label = Uri.EscapeDataString(issuer) + ":" + Uri.EscapeDataString(user.UserName!);
        var uri = $"otpauth://totp/{label}?secret={key}&issuer={issuer}&algorithm=SHA1&digits=6&period=30";
        return new(key, uri);
    }

    public async Task Enable2FAAsync(string code)
    {
        var user = await CurrentUserAsync();
        if (await users.IsLockedOutAsync(user)) { Logout(); throw new UnauthorizedAccessException("Учётная запись заблокирована."); }
        if (await users.GetTwoFactorEnabledAsync(user)) throw new InvalidOperationException("2FA уже включена.");
        if (!await VerifyCodeAsync(user, code))
        {
            if ((await FailedAsync(user)).Status == LoginStatus.LockedOut) Logout();
            throw new UnauthorizedAccessException("Неверный код аутентификатора.");
        }
        EnsureSuccess(await users.SetTwoFactorEnabledAsync(user, true));
        EnsureSuccess(await users.ResetAccessFailedCountAsync(user));
        logger.LogInformation("Two-factor authentication enabled for {UserId}", user.Id);
    }

    public void Logout() { pendingUserId = null; session.Clear(); }

    private async Task<ApplicationUser> CurrentUserAsync() =>
        await users.FindByIdAsync(session.RequireUser().Id)
        ?? throw new UnauthorizedAccessException("Пользователь не найден.");

    private async Task<bool> VerifyCodeAsync(ApplicationUser user, string code)
    {
        var normalized = (code ?? "").Replace(" ", "").Replace("-", "");
        return Regex.IsMatch(normalized, @"^[0-9]{6}$")
            && await users.VerifyTwoFactorTokenAsync(user, users.Options.Tokens.AuthenticatorTokenProvider, normalized);
    }

    private async Task<AuthResult> FailedAsync(ApplicationUser user)
    {
        EnsureSuccess(await users.AccessFailedAsync(user));
        var locked = await users.IsLockedOutAsync(user);
        logger.LogWarning("Failed authentication for {UserId}; locked: {Locked}", user.Id, locked);
        return new(locked ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials);
    }

    private async Task<AuthResult> CompleteLoginAsync(ApplicationUser user)
    {
        EnsureSuccess(await users.ResetAccessFailedCountAsync(user));
        pendingUserId = null;
        session.SignIn(new(user.Id, user.UserName!));
        logger.LogInformation("User {UserId} signed in", user.Id);
        return new(LoginStatus.Succeeded);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
