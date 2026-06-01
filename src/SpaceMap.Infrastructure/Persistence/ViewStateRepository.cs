using System.Text.Json;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class ViewStateRepository(SqliteConnectionFactory connectionFactory)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(ViewState viewState, CancellationToken cancellationToken = default)
    {
        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    INSERT INTO view_states (
                        scan_id, current_path, breadcrumb_paths_json, sort_mode,
                        minimum_size_bytes, selected_path, restored_at
                    ) VALUES (
                        $scanId, $currentPath, $breadcrumbPathsJson, $sortMode,
                        $minimumSizeBytes, $selectedPath, $restoredAt
                    )
                    ON CONFLICT(scan_id) DO UPDATE SET
                        current_path = excluded.current_path,
                        breadcrumb_paths_json = excluded.breadcrumb_paths_json,
                        sort_mode = excluded.sort_mode,
                        minimum_size_bytes = excluded.minimum_size_bytes,
                        selected_path = excluded.selected_path,
                        restored_at = excluded.restored_at;
                    """;
                command.Parameters.AddWithValue("$scanId", viewState.ScanId);
                command.Parameters.AddWithValue("$currentPath", viewState.CurrentPath);
                command.Parameters.AddWithValue("$breadcrumbPathsJson", JsonSerializer.Serialize(viewState.BreadcrumbPaths, JsonOptions));
                command.Parameters.AddWithValue("$sortMode", viewState.SortMode.ToString());
                command.Parameters.AddWithValue("$minimumSizeBytes", (object?)viewState.MinimumSizeBytes ?? DBNull.Value);
                command.Parameters.AddWithValue("$selectedPath", (object?)viewState.SelectedPath ?? DBNull.Value);
                command.Parameters.AddWithValue("$restoredAt", viewState.RestoredAt.ToString("O"));
                await command.ExecuteNonQueryAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<ViewState?> GetAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM view_states WHERE scan_id = $scanId LIMIT 1;";
                command.Parameters.AddWithValue("$scanId", scanId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    return null;
                }

                var breadcrumbs = JsonSerializer.Deserialize<List<string>>(
                    reader.GetString(reader.GetOrdinal("breadcrumb_paths_json")),
                    JsonOptions) ?? [];
                return new ViewState(
                    scanId,
                    reader.GetString(reader.GetOrdinal("current_path")),
                    breadcrumbs,
                    Enum.Parse<SortMode>(reader.GetString(reader.GetOrdinal("sort_mode"))),
                    reader.IsDBNull(reader.GetOrdinal("minimum_size_bytes")) ? null : reader.GetInt64(reader.GetOrdinal("minimum_size_bytes")),
                    reader.IsDBNull(reader.GetOrdinal("selected_path")) ? null : reader.GetString(reader.GetOrdinal("selected_path")),
                    DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("restored_at"))));
            },
            cancellationToken);
    }
}
