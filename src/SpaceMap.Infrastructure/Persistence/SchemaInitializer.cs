namespace SpaceMap.Infrastructure.Persistence;

public sealed class SchemaInitializer(SqliteConnectionFactory connectionFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE IF NOT EXISTS scan_sessions (
                        scan_id TEXT PRIMARY KEY,
                        scope_mode TEXT NOT NULL,
                        root_paths_json TEXT NOT NULL,
                        started_at TEXT NOT NULL,
                        completed_at TEXT NULL,
                        status TEXT NOT NULL,
                        entries_processed INTEGER NOT NULL,
                        is_partial INTEGER NOT NULL,
                        outdated INTEGER NOT NULL,
                        last_error_summary TEXT NULL
                    );

                    CREATE TABLE IF NOT EXISTS path_nodes (
                        node_id TEXT NOT NULL,
                        scan_id TEXT NOT NULL,
                        parent_node_id TEXT NULL,
                        full_path TEXT NOT NULL,
                        display_name TEXT NOT NULL,
                        kind TEXT NOT NULL,
                        depth INTEGER NOT NULL,
                        real_size_bytes INTEGER NOT NULL,
                        logical_size_bytes INTEGER NOT NULL,
                        direct_child_count INTEGER NOT NULL,
                        descendant_count INTEGER NOT NULL,
                        partial INTEGER NOT NULL,
                        PRIMARY KEY (scan_id, node_id)
                    );

                    CREATE INDEX IF NOT EXISTS ix_path_nodes_scan_path ON path_nodes(scan_id, full_path);
                    CREATE INDEX IF NOT EXISTS ix_path_nodes_scan_parent ON path_nodes(scan_id, parent_node_id);

                    CREATE TABLE IF NOT EXISTS omitted_items (
                        omitted_item_id TEXT PRIMARY KEY,
                        scan_id TEXT NOT NULL,
                        full_path TEXT NOT NULL,
                        reason_code TEXT NOT NULL,
                        reason_detail TEXT NULL,
                        affects_partial_result INTEGER NOT NULL,
                        discovered_at TEXT NOT NULL
                    );

                    CREATE INDEX IF NOT EXISTS ix_omitted_items_scan ON omitted_items(scan_id);

                    CREATE TABLE IF NOT EXISTS view_states (
                        scan_id TEXT PRIMARY KEY,
                        current_path TEXT NOT NULL,
                        breadcrumb_paths_json TEXT NOT NULL,
                        sort_mode TEXT NOT NULL,
                        minimum_size_bytes INTEGER NULL,
                        selected_path TEXT NULL,
                        restored_at TEXT NOT NULL
                    );
                    """;
                await command.ExecuteNonQueryAsync(cancellationToken);
            },
            cancellationToken);
    }
}
