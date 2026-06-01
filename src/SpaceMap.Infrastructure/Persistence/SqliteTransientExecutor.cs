using Microsoft.Data.Sqlite;

namespace SpaceMap.Infrastructure.Persistence;

internal static class SqliteTransientExecutor
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromMilliseconds(100),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(1000),
        TimeSpan.FromMilliseconds(2000)
    ];

    public static async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(
            async () =>
            {
                await operation();
                return true;
            },
            cancellationToken);
    }

    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation();
            }
            catch (SqliteException ex) when (IsTransientLock(ex) && attempt < RetryDelays.Length)
            {
                await Task.Delay(RetryDelays[attempt], cancellationToken);
            }
        }
    }

    private static bool IsTransientLock(SqliteException ex) =>
        ex.SqliteErrorCode is 5 or 6;
}
