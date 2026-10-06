using System.Text.Json;
using ApiVault.Application.Abstractions;
using ApiVault.Application.Services;
using ApiVault.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Oracle.EntityFrameworkCore;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run --project scripts/OracleSmoke -- <appsettings.json>");
    return 2;
}

using var settings = JsonDocument.Parse(await File.ReadAllTextAsync(args[0]));
var connectionString = settings.RootElement
    .GetProperty("ConnectionStrings")
    .GetProperty("Oracle")
    .GetString() ?? throw new InvalidOperationException("Oracle connection string is missing.");

var options = new DbContextOptionsBuilder<ApiVaultDbContext>()
    .UseOracle(connectionString, oracle =>
        oracle.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19))
    .Options;

await using var db = new ApiVaultDbContext(options, new SmokeUserContext());
var projects = await new ProjectService(db).GetAllAsync(false, CancellationToken.None);
var vendors = await new VendorService(db).GetAllAsync(false, CancellationToken.None);
var screens = await new SecurityService(db).GetScreensAsync(CancellationToken.None);
var users = await new UserAdministrationService(db, new UnusedPasswordHasher())
    .GetAllAsync(CancellationToken.None);

Console.WriteLine($"Projects: {projects.Count}; vendors: {vendors.Count}; active screens: {screens.Count}");
Console.WriteLine($"Published APIs: {projects.Sum(x => x.PublishedApiCount)}");
Console.WriteLine($"Users: {users.Count}");
return 0;

sealed class SmokeUserContext : IUserContext
{
    public string UserName => "oracle-smoke";
    public string? IpAddress => null;
    public string? CorrelationId => null;
}

sealed class UnusedPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => throw new NotSupportedException();
    public bool Verify(string password, string passwordHash) => throw new NotSupportedException();
}
