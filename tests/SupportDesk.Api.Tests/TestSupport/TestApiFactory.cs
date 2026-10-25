using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SupportDesk.Api.Features.Ai;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>
/// The real API in memory (team practice: a few WebApplicationFactory tests per endpoint group), with its own
/// database, the AI fake in place of Groq, and a controllable AI budget. Create one per test so tests never share
/// data. By default the database is SQLite in-memory on one shared connection (fast, but one connection object
/// cannot serve parallel requests); <c>useFileDatabase</c> gives a temporary SQLite file instead, where every
/// request opens its own connection exactly as in production, for concurrency tests.
/// </summary>
public sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection? _connection;
    private readonly string? _databaseFile;

    public TestApiFactory(bool useFileDatabase = false)
    {
        if (useFileDatabase)
        {
            _databaseFile = Path.Combine(Path.GetTempPath(), $"supportdesk-test-{Guid.NewGuid():N}.db");
        }
        else
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }
    }

    public FakeAiTicketService Ai { get; } = new();

    public FakeAiCallLimiter Limiter { get; } = new();

    /// <summary>Runs <paramref name="action"/> with a database context in its own scope.</summary>
    public async Task WithDbAsync(Func<SupportDeskDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>();
        await action(db);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Make sure no real key can ever be used, even if user secrets hold one.
        builder.UseSetting("Ai:Groq:ApiKey", string.Empty);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<SupportDeskDbContext>>();
            services.AddDbContext<SupportDeskDbContext>(options =>
            {
                if (_connection is not null)
                {
                    options.UseSqlite(_connection);
                }
                else
                {
                    options.UseSqlite($"Data Source={_databaseFile}");
                }
            });

            services.RemoveAll<IAiTicketService>();
            services.AddSingleton<IAiTicketService>(Ai);

            services.RemoveAll<IAiCallLimiter>();
            services.AddSingleton<IAiCallLimiter>(Limiter);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Test-only schema creation; the real app relies on `dotnet ef database update`.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SupportDeskDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection?.Dispose();
            if (_databaseFile is not null)
            {
                SqliteConnection.ClearAllPools();
                foreach (var file in new[] { _databaseFile, _databaseFile + "-wal", _databaseFile + "-shm" })
                {
                    File.Delete(file);
                }
            }
        }
    }
}
