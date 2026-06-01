using Microsoft.Data.Sqlite;
using SpaceMap.Core.Domain;

namespace SpaceMap.Infrastructure.Persistence;

public sealed class PathNodeRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task DeleteNodesAsync(string scanId, CancellationToken cancellationToken = default)
    {
        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM path_nodes WHERE scan_id = $scanId;";
                command.Parameters.AddWithValue("$scanId", scanId);
                await command.ExecuteNonQueryAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task AppendNodesAsync(
        IReadOnlyList<PathNode> nodes,
        CancellationToken cancellationToken = default)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
                await using var insertCommand = connection.CreateCommand();
                insertCommand.Transaction = transaction;
                insertCommand.CommandText =
                    """
                    INSERT INTO path_nodes (
                        node_id, scan_id, parent_node_id, full_path, display_name, kind, depth,
                        real_size_bytes, logical_size_bytes, direct_child_count, descendant_count, partial
                    ) VALUES (
                        $nodeId, $scanId, $parentNodeId, $fullPath, $displayName, $kind, $depth,
                        $realSizeBytes, $logicalSizeBytes, $directChildCount, $descendantCount, $partial
                    );
                    """;

                var nodeId = insertCommand.Parameters.Add("$nodeId", SqliteType.Text);
                var parameterScanId = insertCommand.Parameters.Add("$scanId", SqliteType.Text);
                var parentNodeId = insertCommand.Parameters.Add("$parentNodeId", SqliteType.Text);
                var fullPath = insertCommand.Parameters.Add("$fullPath", SqliteType.Text);
                var displayName = insertCommand.Parameters.Add("$displayName", SqliteType.Text);
                var kind = insertCommand.Parameters.Add("$kind", SqliteType.Text);
                var depth = insertCommand.Parameters.Add("$depth", SqliteType.Integer);
                var realSizeBytes = insertCommand.Parameters.Add("$realSizeBytes", SqliteType.Integer);
                var logicalSizeBytes = insertCommand.Parameters.Add("$logicalSizeBytes", SqliteType.Integer);
                var directChildCount = insertCommand.Parameters.Add("$directChildCount", SqliteType.Integer);
                var descendantCount = insertCommand.Parameters.Add("$descendantCount", SqliteType.Integer);
                var partial = insertCommand.Parameters.Add("$partial", SqliteType.Integer);

                foreach (var node in nodes)
                {
                    nodeId.Value = node.NodeId;
                    parameterScanId.Value = node.ScanId;
                    parentNodeId.Value = (object?)node.ParentNodeId ?? DBNull.Value;
                    fullPath.Value = node.FullPath;
                    displayName.Value = node.DisplayName;
                    kind.Value = node.Kind.ToString();
                    depth.Value = node.Depth;
                    realSizeBytes.Value = node.RealSizeBytes;
                    logicalSizeBytes.Value = node.LogicalSizeBytes;
                    directChildCount.Value = node.DirectChildCount;
                    descendantCount.Value = node.DescendantCount;
                    partial.Value = node.Partial ? 1 : 0;

                    await insertCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            },
            cancellationToken);
    }

    public async Task<PathNode?> FindByPathAsync(string scanId, string fullPath, CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                return await FindByPathAsync(connection, scanId, fullPath, cancellationToken);
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PathNode>> ListChildrenAsync(
        string scanId,
        string path,
        SortMode sortMode,
        long? minimumSizeBytes,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                var items = new List<PathNode>();
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

                string? parentNodeId = null;
                if (!string.Equals(path, "Local drives", StringComparison.OrdinalIgnoreCase))
                {
                    var node = await FindByPathAsync(connection, scanId, path, cancellationToken);
                    parentNodeId = node?.NodeId;
                    if (node is null)
                    {
                        return (IReadOnlyList<PathNode>)items;
                    }
                }

                await using var command = connection.CreateCommand();
                var orderBy = sortMode switch
                {
                    SortMode.LogicalDesc => "logical_size_bytes DESC, display_name ASC",
                    SortMode.NameAsc => "display_name ASC",
                    _ => "real_size_bytes DESC, display_name ASC"
                };

                command.CommandText =
                    $"""
                    SELECT * FROM path_nodes
                    WHERE scan_id = $scanId
                      AND {GetParentClause(path)}
                      AND ($minimumSizeBytes IS NULL OR real_size_bytes >= $minimumSizeBytes)
                    ORDER BY {orderBy}
                    LIMIT $limit OFFSET $offset;
                    """;
                command.Parameters.AddWithValue("$scanId", scanId);
                command.Parameters.AddWithValue("$parentNodeId", (object?)parentNodeId ?? DBNull.Value);
                command.Parameters.AddWithValue("$minimumSizeBytes", (object?)minimumSizeBytes ?? DBNull.Value);
                command.Parameters.AddWithValue("$limit", limit);
                command.Parameters.AddWithValue("$offset", offset);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(ReadNode(reader));
                }

                return (IReadOnlyList<PathNode>)items;
            },
            cancellationToken);
    }

    public async Task<int> CountChildrenAsync(
        string scanId,
        string path,
        long? minimumSizeBytes,
        CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                string? parentNodeId = null;
                if (!string.Equals(path, "Local drives", StringComparison.OrdinalIgnoreCase))
                {
                    var node = await FindByPathAsync(connection, scanId, path, cancellationToken);
                    parentNodeId = node?.NodeId;
                    if (node is null)
                    {
                        return 0;
                    }
                }

                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    SELECT COUNT(*) FROM path_nodes
                    WHERE scan_id = $scanId
                      AND {GetParentClause(path)}
                      AND ($minimumSizeBytes IS NULL OR real_size_bytes >= $minimumSizeBytes);
                    """;
                command.Parameters.AddWithValue("$scanId", scanId);
                command.Parameters.AddWithValue("$parentNodeId", (object?)parentNodeId ?? DBNull.Value);
                command.Parameters.AddWithValue("$minimumSizeBytes", (object?)minimumSizeBytes ?? DBNull.Value);
                return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<PathNode>> GetRootNodesAsync(string scanId, CancellationToken cancellationToken = default)
    {
        return await SqliteTransientExecutor.ExecuteAsync(
            async () =>
            {
                var items = new List<PathNode>();
                await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM path_nodes WHERE scan_id = $scanId AND parent_node_id IS NULL ORDER BY full_path ASC;";
                command.Parameters.AddWithValue("$scanId", scanId);
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(ReadNode(reader));
                }

                return (IReadOnlyList<PathNode>)items;
            },
            cancellationToken);
    }

    private static async Task<PathNode?> FindByPathAsync(
        SqliteConnection connection,
        string scanId,
        string fullPath,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM path_nodes WHERE scan_id = $scanId AND full_path = $fullPath LIMIT 1;";
        command.Parameters.AddWithValue("$scanId", scanId);
        command.Parameters.AddWithValue("$fullPath", fullPath);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadNode(reader) : null;
    }

    private static string GetParentClause(string path) =>
        string.Equals(path, "Local drives", StringComparison.OrdinalIgnoreCase)
            ? "parent_node_id IS NULL"
            : "parent_node_id = $parentNodeId";

    private static PathNode ReadNode(Microsoft.Data.Sqlite.SqliteDataReader reader) =>
        new(
            reader.GetString(reader.GetOrdinal("node_id")),
            reader.GetString(reader.GetOrdinal("scan_id")),
            reader.IsDBNull(reader.GetOrdinal("parent_node_id")) ? null : reader.GetString(reader.GetOrdinal("parent_node_id")),
            reader.GetString(reader.GetOrdinal("full_path")),
            reader.GetString(reader.GetOrdinal("display_name")),
            Enum.Parse<NodeKind>(reader.GetString(reader.GetOrdinal("kind"))),
            reader.GetInt32(reader.GetOrdinal("depth")),
            reader.GetInt64(reader.GetOrdinal("real_size_bytes")),
            reader.GetInt64(reader.GetOrdinal("logical_size_bytes")),
            reader.GetInt32(reader.GetOrdinal("direct_child_count")),
            reader.GetInt32(reader.GetOrdinal("descendant_count")),
            reader.GetInt64(reader.GetOrdinal("partial")) == 1);
}
