using Microsoft.Data.Sqlite;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class SqliteConnectionFactory(AppDataPaths paths)
{
    private const int DefaultCommandTimeoutSeconds = 30;
    private const int BusyTimeoutMilliseconds = 30000;

    public AppDataPaths Paths => paths;

    public SqliteConnection CreateConnection()
    {
        Directory.CreateDirectory(paths.BaseDirectory);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = paths.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false,
            DefaultTimeout = DefaultCommandTimeoutSeconds
        };

        return new SqliteConnection(builder.ConnectionString);
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await ConfigureConnectionAsync(connection, cancellationToken);
        return connection;
    }

    private static async Task ConfigureConnectionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandTimeout = DefaultCommandTimeoutSeconds;
        command.CommandText =
            $"""
            PRAGMA journal_mode = WAL;
            PRAGMA synchronous = NORMAL;
            PRAGMA temp_store = MEMORY;
            PRAGMA busy_timeout = {BusyTimeoutMilliseconds};
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
