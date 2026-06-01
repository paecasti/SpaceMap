using System.Text.Json;
using SpaceMap.Core.Domain;
using Microsoft.Data.Sqlite;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class ScanSessionRepository(SqliteConnectionFactory connectionFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task UpsertAsync(ScanSession session, CancellationToken cancellationToken = default)
    {
        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO scan_sessions (
                        scan_id, scope_mode, root_paths_json, started_at, completed_at, status,
                        entries_processed, is_partial, outdated, last_error_summary
                    ) VALUES (
                        $scanId, $scopeMode, $rootPaths, $startedAt, $completedAt, $status,
                        $entriesProcessed, $isPartial, $outdated, $lastErrorSummary
                    )
                    ON CONFLICT(scan_id) DO UPDATE SET
                        scope_mode = excluded.scope_mode,
                        root_paths_json = excluded.root_paths_json,
                        started_at = excluded.started_at,
                        completed_at = excluded.completed_at,
                        status = excluded.status,
                        entries_processed = excluded.entries_processed,
                        is_partial = excluded.is_partial,
                        outdated = excluded.outdated,
                        last_error_summary = excluded.last_error_summary;
                    """;
                BindSession(command, session);
                await command.ExecuteNonQueryAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<ScanSession?> GetAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM scan_sessions WHERE scan_id = $scanId LIMIT 1;";
                command.Parameters.AddWithValue("$scanId", scanId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                return ReadSession(reader);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetRootPathsAsync(string scanId, CancellationToken cancellationToken = default)
    {
        var session = await GetAsync(scanId, cancellationToken);
        return session?.Scope.RootPaths ?? Array.Empty<string>();
    }

    public async Task ReplaceOmittedItemsAsync(
        string scanId,
        IReadOnlyList<OmittedItem> items,
        CancellationToken cancellationToken = default)
    {
        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var transaction = (Microsoft.Data.Sqlite.SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

                await using (var deleteCommand = connection.CreateCommand())
                {
                    deleteCommand.Transaction = transaction;
                    deleteCommand.CommandText = "DELETE FROM omitted_items WHERE scan_id = $scanId;";
                    deleteCommand.Parameters.AddWithValue("$scanId", scanId);
                    await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                foreach (var item in items)
                {
                    await using var insertCommand = connection.CreateCommand();
                    insertCommand.Transaction = transaction;
                    insertCommand.CommandText =
                        """
                        INSERT INTO omitted_items (
                            omitted_item_id, scan_id, full_path, reason_code, reason_detail,
                            affects_partial_result, discovered_at
                        ) VALUES (
                            $id, $scanId, $fullPath, $reasonCode, $reasonDetail,
                            $affectsPartialResult, $discoveredAt
                        );
                        """;
                    insertCommand.Parameters.AddWithValue("$id", item.OmittedItemId);
                    insertCommand.Parameters.AddWithValue("$scanId", item.ScanId);
                    insertCommand.Parameters.AddWithValue("$fullPath", item.FullPath);
                    insertCommand.Parameters.AddWithValue("$reasonCode", item.ReasonCode);
                    insertCommand.Parameters.AddWithValue("$reasonDetail", (object?)item.ReasonDetail ?? DBNull.Value);
                    insertCommand.Parameters.AddWithValue("$affectsPartialResult", item.AffectsPartialResult ? 1 : 0);
                    insertCommand.Parameters.AddWithValue("$discoveredAt", item.DiscoveredAt.ToString("O"));
                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<OmittedItem>> GetOmittedItemsAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                var results = new List<OmittedItem>();
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM omitted_items WHERE scan_id = $scanId ORDER BY discovered_at ASC;";
                command.Parameters.AddWithValue("$scanId", scanId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(
                        new OmittedItem(
                            reader.GetString(reader.GetOrdinal("omitted_item_id")),
                            reader.GetString(reader.GetOrdinal("scan_id")),
                            reader.GetString(reader.GetOrdinal("full_path")),
                            reader.GetString(reader.GetOrdinal("reason_code")),
                            reader.IsDBNull(reader.GetOrdinal("reason_detail")) ? null : reader.GetString(reader.GetOrdinal("reason_detail")),
                            reader.GetInt64(reader.GetOrdinal("affects_partial_result")) == 1,
                            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("discovered_at")))));
                }

                return (IReadOnlyList<OmittedItem>)results;
            },
            cancellationToken);
    }

    private static void BindSession(SqliteCommand command, ScanSession session)
    {
        command.Parameters.AddWithValue("$scanId", session.ScanId);
        command.Parameters.AddWithValue("$scopeMode", session.Scope.Mode.ToString());
        command.Parameters.AddWithValue("$rootPaths", JsonSerializer.Serialize(session.Scope.RootPaths, JsonOptions));
        command.Parameters.AddWithValue("$startedAt", session.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("$completedAt", session.CompletedAt?.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$status", session.Status.ToString());
        command.Parameters.AddWithValue("$entriesProcessed", session.EntriesProcessed);
        command.Parameters.AddWithValue("$isPartial", session.IsPartial ? 1 : 0);
        command.Parameters.AddWithValue("$outdated", session.Outdated ? 1 : 0);
        command.Parameters.AddWithValue("$lastErrorSummary", (object?)session.LastErrorSummary ?? DBNull.Value);
    }

    private static ScanSession ReadSession(SqliteDataReader reader)
    {
        var scanId = reader.GetString(reader.GetOrdinal("scan_id"));
        var scopeMode = Enum.Parse<ScopeMode>(reader.GetString(reader.GetOrdinal("scope_mode")));
        var rootPaths = JsonSerializer.Deserialize<List<string>>(reader.GetString(reader.GetOrdinal("root_paths_json")), JsonOptions) ?? [];
        return new ScanSession(
            scanId,
            new ScanScope(scopeMode, rootPaths),
            DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("started_at"))),
            reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("completed_at"))),
            Enum.Parse<ScanStatus>(reader.GetString(reader.GetOrdinal("status"))),
            reader.GetInt64(reader.GetOrdinal("entries_processed")),
            reader.GetInt64(reader.GetOrdinal("is_partial")) == 1,
            reader.GetInt64(reader.GetOrdinal("outdated")) == 1,
            reader.IsDBNull(reader.GetOrdinal("last_error_summary")) ? null : reader.GetString(reader.GetOrdinal("last_error_summary")));
    }
}
