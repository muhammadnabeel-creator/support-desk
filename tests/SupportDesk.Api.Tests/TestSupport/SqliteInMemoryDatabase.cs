using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>
/// A private SQLite in-memory database for one test. The connection is held open for the
/// lifetime of this object, because SQLite drops an in-memory database when its last
/// connection closes. The schema is created with <c>EnsureCreated</c> (test-only).
/// </summary>
public sealed class SqliteInMemoryDatabase : IDisposable
{
    private readonly DbContextOptions<SupportDeskDbContext> _options;

    public SqliteInMemoryDatabase()
    {
        Connection = new SqliteConnection("DataSource=:memory:");
        Connection.Open();

        _options = new DbContextOptionsBuilder<SupportDeskDbContext>()
            .UseSqlite(Connection)
            .Options;

        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    /// <summary>The open connection, for raw ADO.NET checks of stored values.</summary>
    public SqliteConnection Connection { get; }

    /// <summary>Creates a new context on the shared connection (a fresh change tracker each time).</summary>
    public SupportDeskDbContext CreateContext() => new(_options);

    public void Dispose() => Connection.Dispose();
}
