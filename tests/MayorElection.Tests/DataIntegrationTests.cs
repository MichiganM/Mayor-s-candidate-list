using MayorElection.Data;
using MayorElection.Data.Repositories;
using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MayorElection.Tests;

public sealed class DataIntegrationTests
{
    [Fact]
    public async Task Migration_persists_hierarchy_and_repository_filters_in_position_order()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using (var context = new AppDbContext(options))
        {
            await context.Database.MigrateAsync();
            Assert.Equal(3, await context.CandidateStatuses.CountAsync());
            context.AddRange(
                new SelfNominee { FirstName = "Иван", LastName = "Иванов", Position = 2 },
                new PartyCandidate { FirstName = "Анна", LastName = "Петрова", Position = 1, PartyName = "Партия" },
                new ElectionCommissionMember { FirstName = "Пётр", LastName = "Сидоров", CommissionName = "Комиссия", Role = "Секретарь" });
            await context.SaveChangesAsync();
        }
        await using var read = new AppDbContext(options);
        var repository = new CandidateRepository(read);
        var candidates = await repository.ListAsync();
        Assert.Equal(new[] { 1, 2 }, candidates.Select(x => x.Position));
        Assert.IsType<PartyCandidate>(candidates[0]);
        Assert.IsType<SelfNominee>(candidates[1]);
        Assert.Single(await repository.ListAsync(CandidateStatus.Registered, CandidateTemplate.PartyCandidate));
        Assert.Empty(await repository.ListAsync(CandidateStatus.Withdrawn));
        Assert.Single(await read.CommissionMembers.ToListAsync());
    }

    [Fact]
    public async Task Database_rejects_duplicate_positions_and_transaction_rolls_back()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);
        await context.Database.MigrateAsync();
        var unit = new UnitOfWork(context);
        await Assert.ThrowsAsync<DbUpdateException>(() => unit.ExecuteInTransactionAsync(async token =>
        {
            context.Add(new SelfNominee { FirstName = "Иван", LastName = "Иванов", Position = 1 });
            await unit.SaveChangesAsync(token);
            context.Add(new SelfNominee { FirstName = "Анна", LastName = "Петрова", Position = 1 });
            await unit.SaveChangesAsync(token);
        }));
        Assert.Empty(await context.Candidates.ToListAsync());
    }

    [Fact]
    public async Task Bootstrap_creates_admin_once_with_identity_hash_and_role()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ElectionDatabase"] = "Data Source=:memory:"
        }).Build();
        await using var provider = new ServiceCollection().AddElectionData(configuration).BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.OpenConnectionAsync();
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        string? credentials = null;
        await initializer.InitializeAsync(value => credentials = value);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = Assert.IsType<ApplicationUser>(await users.FindByNameAsync("admin"));
        var password = credentials!.Split("Password: ")[1];
        Assert.NotEqual(password, admin.PasswordHash);
        Assert.True(await users.CheckPasswordAsync(admin, password));
        Assert.True(await users.IsInRoleAsync(admin, "Admin"));
        Assert.True(admin.LockoutEnabled);
        await initializer.InitializeAsync(_ => throw new InvalidOperationException("Credentials must not be reprinted."));
        Assert.Single(await context.Users.ToListAsync());
    }
}
