using FluentValidation;
using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using MayorElection.Domain.Repositories;
using MayorElection.Services;
using MayorElection.Services.DTOs;
using MayorElection.Services.Validation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace MayorElection.Tests;

/// <summary>Service tests without SQLite, filesystem, Windows forms, or a DI host.</summary>
public sealed class UnitTests
{
    [Fact]
    public async Task Invalid_party_candidate_is_rejected_before_repository_write()
    {
        using var fixture = new Fixture();
        await fixture.SignInAsync();
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.AddAsync(Valid() with
        { Template = CandidateTemplate.PartyCandidate, PartyName = " " }));
        Assert.Empty(fixture.Store.Items);
        Assert.Equal(0, fixture.Store.Saves);
    }

    [Fact]
    public async Task Status_change_preserves_registration_and_identity()
    {
        using var fixture = new Fixture();
        await fixture.SignInAsync();
        var original = await fixture.Service.AddAsync(Valid());
        await fixture.Service.ChangeStatusAsync(original.Id, CandidateStatus.Rejected);
        Assert.Equal(original with { Status = CandidateStatus.Rejected }, await fixture.Service.GetAsync(original.Id));
    }

    [Fact]
    public async Task Moving_down_changes_order_without_changing_candidate_composition()
    {
        using var fixture = new Fixture();
        await fixture.SignInAsync();
        var first = await fixture.Service.AddAsync(Valid());
        var second = await fixture.Service.AddAsync(Valid() with { LastName = "Петров" });
        await fixture.Order.MoveAsync(first.Id, MoveDirection.Down);
        var list = await fixture.Service.ListAsync();
        Assert.Equal(new[] { second.Id, first.Id }, list.Select(x => x.Id));
        Assert.Equal(new[] { 1, 2 }, list.Select(x => x.Position));
        Assert.Equal(first with { Position = 2 }, list[1]);
    }

    [Fact]
    public void Identity_hash_is_salted_and_never_equals_plaintext()
    {
        var hasher = new PasswordHasher<ApplicationUser>(Options.Create(new PasswordHasherOptions { IterationCount = 210_000 }));
        var user = new ApplicationUser();
        const string password = "Unit-Test-Password-123!";
        var first = hasher.HashPassword(user, password);
        var second = hasher.HashPassword(user, password);
        Assert.NotEqual(password, first);
        Assert.NotEqual(first, second);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, first, password));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, first, "wrong"));
    }

    [Fact]
    public async Task Identity_authenticator_accepts_current_code_and_rejects_invalid_six_digit_code()
    {
        using var users = new StubUserManager();
        var provider = new AuthenticatorTokenProvider<ApplicationUser>();
        var valid = ServiceTests.Totp(users.Key);
        Assert.True(await provider.ValidateAsync("TwoFactor", valid, users, users.User));
        // Find a numeric code outside the provider's accepted time window, avoiding a probabilistic test.
        var invalid = "000000";
        while (await provider.ValidateAsync("TwoFactor", invalid, users, users.User))
            invalid = (int.Parse(invalid) + 1).ToString("D6");
        Assert.False(await provider.ValidateAsync("TwoFactor", invalid, users, users.User));
    }

    [Fact]
    public void Validator_uses_injected_date_and_accepts_hyphenated_names_without_patronymic()
    {
        var clock = new FixedClock();
        var validator = new CandidateValidator(clock);
        var candidate = Valid() with { LastName = "Иванова-Петрова", Patronymic = null, RegistrationDate = new(2026, 11, 15) };
        Assert.True(validator.Validate(candidate).IsValid);
        var invalid = validator.Validate(candidate with { RegistrationDate = new(2026, 11, 16) });
        Assert.Contains(invalid.Errors, x => x.PropertyName == nameof(CandidateDto.RegistrationDate));
    }

    private static CandidateDto Valid() => new()
    {
        LastName = "Иванов", FirstName = "Иван", BirthDate = new(1980, 1, 1), RegistrationDate = new(2020, 1, 1)
    };

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class Fixture : IDisposable
    {
        private readonly StubUserManager users = new();
        private readonly AuthService auth;
        public MemoryStore Store { get; } = new();
        public CandidateService Service { get; }
        public ListOrderService Order { get; }

        public Fixture()
        {
            var session = new UserSession();
            auth = new AuthService(users, session, new FixedClock(), NullLogger<AuthService>.Instance);
            Order = new(Store, Store, session);
            Service = new(Store, Store, new CandidateValidator(new FixedClock()), Order, session, NullLogger<CandidateService>.Instance);
        }
        public async Task SignInAsync() => Assert.Equal(LoginStatus.Succeeded, (await auth.LoginAsync("unit", "test")).Status);
        public void Dispose() => users.Dispose();
    }

    // This double supplies data only; SQLite integrity and transaction semantics are tested separately.
    private sealed class MemoryStore : ICandidateRepository, IUnitOfWork
    {
        public List<Candidate> Items { get; } = new();
        public int Saves { get; private set; }
        public Task<IReadOnlyList<Candidate>> ListAsync(CandidateStatus? status = null,
            CandidateTemplate? template = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Candidate>>(Items.Where(x => (!status.HasValue || x.Status == status)
                && (!template.HasValue || x.Template == template)).OrderBy(x => x.Position).ToArray());
        public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.SingleOrDefault(x => x.Id == id));
        public Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default)
        { Items.Add(candidate); return Task.CompletedTask; }
        public void Remove(Candidate candidate) => Items.Remove(candidate);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.FromResult(1); }
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
    }

    private sealed class StubUserManager() : UserManager<ApplicationUser>(new UnusedStore(), Microsoft.Extensions.Options.Options.Create(new IdentityOptions()),
        new PasswordHasher<ApplicationUser>(), [], [], new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(),
        null!, NullLogger<UserManager<ApplicationUser>>.Instance)
    {
        public ApplicationUser User { get; } = new() { UserName = "unit" };
        public string Key => "JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP";
        public override Task<ApplicationUser?> FindByNameAsync(string userName) => Task.FromResult<ApplicationUser?>(User);
        public override Task<bool> CheckPasswordAsync(ApplicationUser user, string password) => Task.FromResult(password == "test");
        public override Task<bool> IsLockedOutAsync(ApplicationUser user) => Task.FromResult(false);
        public override Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user) => Task.FromResult(false);
        public override Task<IdentityResult> ResetAccessFailedCountAsync(ApplicationUser user) => Task.FromResult(IdentityResult.Success);
        public override Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user) => Task.FromResult<string?>(Key);
    }

    private sealed class UnusedStore : IUserStore<ApplicationUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
