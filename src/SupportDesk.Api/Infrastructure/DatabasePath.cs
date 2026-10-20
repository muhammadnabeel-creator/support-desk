using Microsoft.Data.Sqlite;

namespace SupportDesk.Api.Infrastructure;

/// <summary>
/// Pins the SQLite database file to a fixed location. A relative <c>Data Source</c> is resolved
/// against the content root (the API project folder), so <c>dotnet run</c> and
/// <c>dotnet ef database update</c> always use the same file whatever the working directory is.
/// </summary>
public static class DatabasePath
{
    private const string InMemoryDataSource = ":memory:";

    /// <summary>
    /// Returns <paramref name="connectionString"/> with an absolute <c>Data Source</c> and makes sure
    /// the folder that will hold the database file exists. In-memory databases are returned unchanged.
    /// </summary>
    /// <param name="connectionString">SQLite connection string, e.g. <c>Data Source=App_Data/supportdesk.db</c>.</param>
    /// <param name="contentRootPath">Absolute path that relative data sources are resolved against.</param>
    /// <exception cref="InvalidOperationException">The connection string or its data source is empty.</exception>
    public static string ResolveConnectionString(string? connectionString, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The SQLite connection string 'ConnectionStrings:SupportDesk' is missing or empty.");
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource))
        {
            throw new InvalidOperationException(
                "The SQLite connection string 'ConnectionStrings:SupportDesk' has no Data Source.");
        }

        if (IsInMemory(builder))
        {
            return connectionString;
        }

        var isRelative = !Path.IsPathRooted(builder.DataSource);
        if (isRelative)
        {
            builder.DataSource = Path.GetFullPath(Path.Combine(contentRootPath, builder.DataSource));
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // An absolute data source is returned exactly as configured.
        return isRelative ? builder.ToString() : connectionString;
    }

    private static bool IsInMemory(SqliteConnectionStringBuilder builder) =>
        builder.Mode == SqliteOpenMode.Memory
        || string.Equals(builder.DataSource, InMemoryDataSource, StringComparison.OrdinalIgnoreCase);
}
