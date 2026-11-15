using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FluentValidation;
using MayorElection.Data;
using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using MayorElection.Services;
using MayorElection.Services.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MayorElection.Tests;

public sealed class ServiceTests
{
    [Fact]
    public async Task Candidate_validation_rejects_bad_name_dates_and_missing_party_before_save()
    {
        await using var app = await TestApp.CreateAsync();
        await Assert.ThrowsAsync<ValidationException>(() => app.Candidates.AddAsync(Valid() with
        {
            FirstName = "123", RegistrationDate = new DateOnly(1900, 1, 1),
            Template = CandidateTemplate.PartyCandidate, PartyName = " "
        }));
        Assert.Empty(await app.Candidates.ListAsync());
    }

    [Fact]
    public async Task Status_change_and_edit_preserve_id_and_position()
    {
        await using var app = await TestApp.CreateAsync();
        var candidate = await app.Candidates.AddAsync(Valid());
        await app.Candidates.ChangeStatusAsync(candidate.Id, CandidateStatus.Withdrawn);
        await app.Candidates.UpdateAsync(candidate.Id, candidate with { LastName = "Петров", Status = CandidateStatus.Withdrawn, Position = 99 });
        var actual = await app.Candidates.GetAsync(candidate.Id);
        Assert.Equal(CandidateStatus.Withdrawn, actual.Status);
        Assert.Equal("Петров", actual.LastName);
        Assert.Equal(1, actual.Position);
        Assert.Single(await app.Candidates.ListAsync(CandidateStatus.Withdrawn));
        await Assert.ThrowsAsync<ValidationException>(() => app.Candidates.ChangeStatusAsync(candidate.Id, (CandidateStatus)99));
    }

    [Fact]
    public async Task Move_and_delete_keep_contiguous_unique_positions()
    {
        await using var app = await TestApp.CreateAsync();
        var first = await app.Candidates.AddAsync(Valid());
        var second = await app.Candidates.AddAsync(Valid() with { Template = CandidateTemplate.PartyCandidate, PartyName = "Партия" });
        var third = await app.Candidates.AddAsync(Valid());
        var order = app.Get<ListOrderService>();
        await order.MoveAsync(third.Id, MoveDirection.Up);
        Assert.Equal(new[] { first.Id, third.Id, second.Id }, (await app.Candidates.ListAsync()).Select(x => x.Id));
        await order.MoveAsync(first.Id, MoveDirection.Up);
        await order.MoveAsync(second.Id, MoveDirection.Down);
        await app.Candidates.DeleteAsync(third.Id);
        Assert.Equal(new[] { 1, 2 }, (await app.Candidates.ListAsync()).Select(x => x.Position));
        var added = await app.Candidates.AddAsync(Valid());
        Assert.Equal(3, added.Position);
    }

    [Fact]
    public async Task Json_round_trip_restores_templates_order_and_status()
    {
        await using var app = await TestApp.CreateAsync();
        var first = await app.Candidates.AddAsync(Valid());
        await app.Candidates.AddAsync(Valid() with { Template = CandidateTemplate.PartyCandidate, PartyName = "Партия", Status = CandidateStatus.Rejected });
        var expected = await app.Candidates.ListAsync();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            await app.Get<ExportService>().SaveJsonAsync(path);
            await app.Candidates.DeleteAsync(first.Id);
            await app.Get<ExportService>().LoadJsonAsync(path);
            Assert.Equal(expected, await app.Candidates.ListAsync());
            // Repeat import with the same ids while old entities have already been tracked.
            await app.Get<ExportService>().LoadJsonAsync(path);
            Assert.Equal(expected, await app.Candidates.ListAsync());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Invalid_json_import_leaves_existing_list_intact()
    {
        await using var app = await TestApp.CreateAsync();
        await app.Candidates.AddAsync(Valid());
        var expected = await app.Candidates.ListAsync();
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            await app.Get<ExportService>().SaveJsonAsync(path);
            var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
            json["Candidates"]![0]!["Position"] = 4;
            await File.WriteAllTextAsync(path, json.ToJsonString());
            await Assert.ThrowsAsync<InvalidDataException>(() => app.Get<ExportService>().LoadJsonAsync(path));
            Assert.Equal(expected, await app.Candidates.ListAsync());
            await File.WriteAllTextAsync(path, "{invalid}");
            await Assert.ThrowsAsync<InvalidDataException>(() => app.Get<ExportService>().LoadJsonAsync(path));
            Assert.Equal(expected, await app.Candidates.ListAsync());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Import_database_failure_rolls_back_deletion_and_scope_remains_usable()
    {
        await using var app = await TestApp.CreateAsync();
        await app.Candidates.AddAsync(Valid());
        var expected = await app.Candidates.ListAsync();
        var context = app.Get<AppDbContext>();
        var member = new ElectionCommissionMember
        {
            FirstName = "Анна", LastName = "Петрова", CommissionName = "Комиссия", Role = "Секретарь"
        };
        context.Add(member);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear(); // Exercise the database primary-key constraint, not EF's identity map.
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            await app.Get<ExportService>().SaveJsonAsync(path);
            var json = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
            json["Candidates"]![0]!["Id"] = member.Id.ToString();
            await File.WriteAllTextAsync(path, json.ToJsonString());
            await Assert.ThrowsAsync<DbUpdateException>(() => app.Get<ExportService>().LoadJsonAsync(path));
            Assert.Equal(expected, await app.Candidates.ListAsync());
            Assert.Equal(2, (await app.Candidates.AddAsync(Valid())).Position);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Editing_template_is_rejected_without_changing_candidate()
    {
        await using var app = await TestApp.CreateAsync();
        var candidate = await app.Candidates.AddAsync(Valid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => app.Candidates.UpdateAsync(candidate.Id,
            candidate with { Template = CandidateTemplate.PartyCandidate, PartyName = "Партия" }));
        Assert.Equal(candidate, await app.Candidates.GetAsync(candidate.Id));
    }

    [Fact]
    public async Task Registration_checks_confirmation_and_stores_only_identity_hash()
    {
        await using var app = await TestApp.CreateAsync();
        var request = new RegisterRequest("another", TestApp.Password, "different");
        await Assert.ThrowsAsync<ValidationException>(() => app.Auth.RegisterAsync(request));
        Assert.Null(await app.Get<UserManager<ApplicationUser>>().FindByNameAsync("another"));
        await app.Auth.RegisterAsync(request with { ConfirmPassword = TestApp.Password });
        var user = (await app.Get<UserManager<ApplicationUser>>().FindByNameAsync("another"))!;
        Assert.NotEqual(TestApp.Password, user.PasswordHash);
        var hasher = app.Get<IPasswordHasher<ApplicationUser>>();
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash!, TestApp.Password));
        Assert.DoesNotContain(TestApp.Password, request.ToString());
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_account_and_block_correct_password()
    {
        await using var app = await TestApp.CreateAsync();
        for (var attempt = 1; attempt <= 5; attempt++)
            Assert.Equal(attempt == 5 ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials,
                (await app.Auth.LoginAsync("tester", "wrong-password")).Status);
        Assert.Equal(LoginStatus.LockedOut, (await app.Auth.LoginAsync("tester", TestApp.Password)).Status);
        Assert.False(app.Get<UserSession>().IsAuthenticated);
        var user = (await app.Get<UserManager<ApplicationUser>>().FindByNameAsync("tester"))!;
        Assert.InRange(user.LockoutEnd!.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(16));
    }

    [Fact]
    public async Task Totp_setup_requires_password_and_login_requires_second_factor()
    {
        await using var app = await TestApp.CreateAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => app.Auth.Begin2FASetupAsync("wrong"));
        var setup = await app.Auth.Begin2FASetupAsync(TestApp.Password);
        Assert.StartsWith("otpauth://totp/MayorElection:tester?secret=", setup.AuthenticatorUri);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => app.Auth.Enable2FAAsync("bad"));
        await app.Auth.Enable2FAAsync(Totp(setup.SharedKey));
        app.Auth.Logout();
        Assert.Equal(LoginStatus.InvalidCredentials, (await app.Auth.Verify2FAAsync(Totp(setup.SharedKey))).Status);
        Assert.Equal(LoginStatus.RequiresTwoFactor, (await app.Auth.LoginAsync("tester", TestApp.Password)).Status);
        Assert.False(app.Get<UserSession>().IsAuthenticated);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => app.Candidates.ListAsync());
        Assert.Equal(LoginStatus.InvalidCredentials, (await app.Auth.Verify2FAAsync("bad")).Status);
        Assert.Equal(LoginStatus.Succeeded, (await app.Auth.Verify2FAAsync(Totp(setup.SharedKey))).Status);
        Assert.True(app.Get<UserSession>().IsAuthenticated);
        Assert.Equal(LoginStatus.InvalidCredentials, (await app.Auth.Verify2FAAsync(Totp(setup.SharedKey))).Status);
    }

    [Fact]
    public async Task Correct_password_does_not_reset_wrong_second_factor_attempts()
    {
        await using var app = await TestApp.CreateAsync();
        var setup = await app.Auth.Begin2FASetupAsync(TestApp.Password);
        await app.Auth.Enable2FAAsync(Totp(setup.SharedKey));
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            Assert.Equal(LoginStatus.RequiresTwoFactor, (await app.Auth.LoginAsync("tester", TestApp.Password)).Status);
            Assert.Equal(attempt == 5 ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials,
                (await app.Auth.Verify2FAAsync("bad")).Status);
        }
        Assert.Equal(LoginStatus.LockedOut, (await app.Auth.LoginAsync("tester", TestApp.Password)).Status);
    }

    [Fact]
    public async Task Expired_two_factor_challenge_cannot_create_session()
    {
        await using var app = await TestApp.CreateAsync();
        var setup = await app.Auth.Begin2FASetupAsync(TestApp.Password);
        await app.Auth.Enable2FAAsync(Totp(setup.SharedKey));
        await app.Auth.LoginAsync("tester", TestApp.Password);
        app.Clock.Offset = TimeSpan.FromMinutes(6);
        Assert.Equal(LoginStatus.InvalidCredentials, (await app.Auth.Verify2FAAsync(Totp(setup.SharedKey))).Status);
        Assert.False(app.Get<UserSession>().IsAuthenticated);
    }

    [Fact]
    public async Task Logout_denies_candidate_operations()
    {
        await using var app = await TestApp.CreateAsync();
        app.Auth.Logout();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => app.Candidates.AddAsync(Valid()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => app.Get<ListOrderService>().MoveAsync(Guid.NewGuid(), MoveDirection.Up));
    }

    private static CandidateDto Valid() => new()
    {
        LastName = "Иванов", FirstName = "Иван", Patronymic = "Иванович",
        BirthDate = new DateOnly(1980, 1, 1), RegistrationDate = new DateOnly(2020, 1, 1)
    };

    // Independent RFC 6238 test-code generator; production verification uses only Identity.
    internal static string Totp(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        var buffer = 0; var bits = 0;
        foreach (var character in secret.TrimEnd('='))
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits >= 8) { bits -= 8; bytes.Add((byte)(buffer >> bits)); }
        }
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var offset = hash[^1] & 15;
        var value = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, 4)) & int.MaxValue;
        return (value % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class TestClock : TimeProvider
    {
        public TimeSpan Offset { get; set; }
        public override DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow + Offset;
    }

    private sealed class TestApp(ServiceProvider provider, AsyncServiceScope scope, TestClock clock) : IAsyncDisposable
    {
        public const string Password = "Test-Password-123!";
        public TestClock Clock => clock;
        public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
        public AuthService Auth => Get<AuthService>();
        public CandidateService Candidates => Get<CandidateService>();

        public static async Task<TestApp> CreateAsync()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:ElectionDatabase"] = "Data Source=:memory:"
            }).Build();
            var clock = new TestClock();
            var services = new ServiceCollection().AddElectionData(configuration);
            services.AddSingleton<TimeProvider>(clock).AddElectionServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
            var app = new TestApp(provider, provider.CreateAsyncScope(), clock);
            var context = app.Get<AppDbContext>();
            await context.Database.OpenConnectionAsync();
            await context.Database.MigrateAsync();
            await app.Auth.RegisterAsync(new("tester", Password, Password));
            Assert.Equal(LoginStatus.Succeeded, (await app.Auth.LoginAsync("tester", Password)).Status);
            return app;
        }

        public async ValueTask DisposeAsync()
        {
            await scope.DisposeAsync();
            await provider.DisposeAsync();
        }
    }
}
