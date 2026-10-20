using Microsoft.Data.Sqlite;
using SupportDesk.Api.Infrastructure;

namespace SupportDesk.Api.Tests.Infrastructure;

/// <summary>
/// <see cref="DatabasePath"/> pins the SQLite file under the content root and creates its folder.
/// Each test works inside its own temporary folder, removed afterwards.
/// </summary>
[Trait("Unit", "U1")]
public sealed class DatabasePathTests : IDisposable
{
    private readonly string _contentRoot =
        Path.Combine(Path.GetTempPath(), "supportdesk-dbpath-" + Guid.NewGuid().ToString("N"));

    public DatabasePathTests()
    {
        Directory.CreateDirectory(_contentRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_contentRoot))
        {
            Directory.Delete(_contentRoot, recursive: true);
        }
    }

    [Fact]
    public void RelativeDataSource_BecomesAbsoluteUnderContentRoot()
    {
        var result = DatabasePath.ResolveConnectionString("Data Source=App_Data/supportdesk.db", _contentRoot);

        var dataSource = new SqliteConnectionStringBuilder(result).DataSource;
        Assert.True(Path.IsPathRooted(dataSource));
        Assert.Equal(Path.Combine(_contentRoot, "App_Data", "supportdesk.db"), dataSource);
    }

    [Fact]
    public void RelativeDataSource_CreatesTheDatabaseFolder()
    {
        var expectedFolder = Path.Combine(_contentRoot, "App_Data");
        Assert.False(Directory.Exists(expectedFolder));

        DatabasePath.ResolveConnectionString("Data Source=App_Data/supportdesk.db", _contentRoot);

        Assert.True(Directory.Exists(expectedFolder));
        Assert.False(File.Exists(Path.Combine(expectedFolder, "supportdesk.db")));
    }

    [Fact]
    public void AbsoluteDataSource_IsLeftUnchanged()
    {
        var absolutePath = Path.Combine(_contentRoot, "elsewhere", "tickets.db");

        var result = DatabasePath.ResolveConnectionString($"Data Source={absolutePath}", "/some/other/root");

        Assert.Equal(absolutePath, new SqliteConnectionStringBuilder(result).DataSource);
    }

    [Fact]
    public void InMemoryDataSource_IsReturnedUnchanged()
    {
        const string connectionString = "DataSource=:memory:";

        var result = DatabasePath.ResolveConnectionString(connectionString, _contentRoot);

        Assert.Equal(connectionString, result);
        Assert.Empty(Directory.EnumerateFileSystemEntries(_contentRoot));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Mode=ReadWrite")]
    public void MissingConnectionStringOrDataSource_Throws(string? connectionString)
    {
        var error = Assert.Throws<InvalidOperationException>(
            () => DatabasePath.ResolveConnectionString(connectionString, _contentRoot));

        Assert.Contains("ConnectionStrings:SupportDesk", error.Message);
    }
}
