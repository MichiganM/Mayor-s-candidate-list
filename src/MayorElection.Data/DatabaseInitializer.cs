using System.Security.Cryptography;
using MayorElection.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MayorElection.Data;

public sealed class DatabaseInitializer(AppDbContext context, UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles)
{
    public async Task InitializeAsync(Action<string> displayInitialCredentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(displayInitialCredentials);
        await context.Database.MigrateAsync(cancellationToken);
        // A single transaction prevents partially created administrator/role state.
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        if (!await roles.RoleExistsAsync("Admin"))
            EnsureSuccess(await roles.CreateAsync(new IdentityRole("Admin")));

        if (await users.FindByNameAsync("admin") is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        // Bootstrap only an empty user store; never grant an existing user admin rights.
        if (await context.Users.AnyAsync(cancellationToken))
            throw new InvalidOperationException("Cannot bootstrap administrator in a non-empty user store.");

        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)) + "aA1!";
        var admin = new ApplicationUser { UserName = "admin", LockoutEnabled = true };
        EnsureSuccess(await users.CreateAsync(admin, password));
        EnsureSuccess(await users.AddToRoleAsync(admin, "Admin"));
        // Display before commit: if output fails, rollback permits a fresh bootstrap on next run.
        displayInitialCredentials($"Initial administrator: admin{Environment.NewLine}Password: {password}");
        await transaction.CommitAsync(cancellationToken);
    }

    private static void EnsureSuccess(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}
