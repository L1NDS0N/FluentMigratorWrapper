using System;
using System.Data;
using Microsoft.Data.SqlClient;
using FluentMigratorWrapper.Services.DatabaseIntrospection.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FluentMigratorWrapper.Services.DatabaseIntrospection;


public class SqlServerIntrospector : IDatabaseIntrospector
{
    private readonly string _connectionString;

    public SqlServerIntrospector(string connectionString)
    {
        _connectionString = connectionString;
    }

        public async Task<string> DetectDefaultSchemaAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Try to get the default schema for the current user
            const string sql = @"SELECT DEFAULT_SCHEMA_NAME FROM sys.database_principals WHERE name = USER_NAME()";
            await using var cmd = new SqlCommand(sql, connection);
            var res = await cmd.ExecuteScalarAsync();
            return res?.ToString() ?? "dbo";
        }

    public async Task<DatabaseInfo> GetDatabaseStructureAsync(string schema, string[]? specificTables = null)
    {
        var dbInfo = new DatabaseInfo { Schema = schema };

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        // Busca todas as tabelas
        var tables = await GetTablesAsync(connection, schema, specificTables);

        foreach (var table in tables)
        {
            // Busca colunas
            table.Columns = await GetColumnsAsync(connection, schema, table.Name);

            // Busca FKs
            table.ForeignKeys = await GetForeignKeysAsync(connection, schema, table.Name);

            // Busca índices
            table.Indexes = await GetIndexesAsync(connection, schema, table.Name);

            dbInfo.Tables.Add(table);
        }

        return dbInfo;
    }

    private async Task<List<TableInfo>> GetTablesAsync(SqlConnection connection, string schema, string[]? specificTables)
    {
        var tables = new List<TableInfo>();
        var sql = @"
            SELECT 
                t.name AS TableName,
                SCHEMA_NAME(t.schema_id) AS SchemaName,
                ep.value AS Description
            FROM sys.tables t
            LEFT JOIN sys.extended_properties ep ON ep.major_id = t.object_id 
                AND ep.minor_id = 0 
                AND ep.name = 'MS_Description'
            WHERE SCHEMA_NAME(t.schema_id) = @Schema";

        if (specificTables?.Length > 0)
        {
            sql += " AND t.name IN @TableNames";
        }

        sql += " ORDER BY t.name";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@Schema", schema);
        
        if (specificTables?.Length > 0)
        {
            var tableParam = cmd.Parameters.AddWithValue("@TableNames", string.Join(",", specificTables));
            tableParam.SqlDbType = SqlDbType.NVarChar;
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(new TableInfo
            {
                Name = reader.GetString(0),
                Schema = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return tables;
    }

    private async Task<List<ColumnInfo>> GetColumnsAsync(SqlConnection connection, string schema, string tableName)
    {
        var columns = new List<ColumnInfo>();
        var sql = @"
            SELECT 
                c.name AS ColumnName,
                t.name AS DataType,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity,
                c.max_length AS MaxLength,
                c.precision AS Precision,
                c.scale AS Scale,
                dc.definition AS DefaultValue,
                ep.value AS Description,
                c.column_id AS ColumnId,
                CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey
            FROM sys.columns c
            INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
            LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
            LEFT JOIN sys.extended_properties ep ON ep.major_id = c.object_id 
                AND ep.minor_id = c.column_id 
                AND ep.name = 'MS_Description'
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.indexes i
                INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
            WHERE c.object_id = OBJECT_ID(@SchemaTable)
            ORDER BY c.column_id";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@SchemaTable", $"{schema}.{tableName}");

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dataType = reader.GetString(1);
            int? maxLength = reader.IsDBNull(4) ? null : reader.GetInt16(4);

            // Ajusta max_length para tipos Unicode (divide por 2)
            if (dataType is "nvarchar" or "nchar" && maxLength.HasValue && maxLength > 0)
            {
                maxLength = maxLength.Value / 2;
            }

            columns.Add(new ColumnInfo
            {
                Name = reader.GetString(0),
                DataType = dataType,
                IsNullable = reader.GetBoolean(2),
                IsIdentity = reader.GetBoolean(3),
                MaxLength = maxLength == -1 ? null : maxLength, // -1 = MAX
                Precision = reader.IsDBNull(5) ? null : reader.GetByte(5),
                Scale = reader.IsDBNull(6) ? null : reader.GetByte(6),
                DefaultValue = reader.IsDBNull(7) ? null : reader.GetString(7),
                Description = reader.IsDBNull(8) ? null : reader.GetString(8),
                ColumnId = reader.GetInt32(9),
                IsPrimaryKey = reader.GetInt32(10) == 1
            });
        }

        return columns;
    }

    private async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(SqlConnection connection, string schema, string tableName)
    {
        var foreignKeys = new List<ForeignKeyInfo>();
        var sql = @"
            SELECT 
                fk.name AS FKName,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
                OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
                SCHEMA_NAME(rt.schema_id) AS ReferencedSchema,
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn,
                fk.delete_referential_action_desc AS OnDelete,
                fk.update_referential_action_desc AS OnUpdate
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables rt ON fk.referenced_object_id = rt.object_id
            WHERE fk.parent_object_id = OBJECT_ID(@SchemaTable)
            ORDER BY fk.name";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@SchemaTable", $"{schema}.{tableName}");

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            foreignKeys.Add(new ForeignKeyInfo
            {
                Name = reader.GetString(0),
                ColumnName = reader.GetString(1),
                ReferencedTable = reader.GetString(2),
                ReferencedSchema = reader.GetString(3),
                ReferencedColumn = reader.GetString(4),
                OnDelete = reader.GetString(5),
                OnUpdate = reader.GetString(6)
            });
        }

        return foreignKeys;
    }

    private async Task<List<IndexInfo>> GetIndexesAsync(SqlConnection connection, string schema, string tableName)
    {
        var indexes = new List<IndexInfo>();
        var sql = @"
            SELECT 
                i.name AS IndexName,
                i.is_unique AS IsUnique,
                i.is_primary_key AS IsPrimaryKey,
                i.type_desc AS IndexType,
                COL_NAME(ic.object_id, ic.column_id) AS ColumnName,
                ic.is_descending_key AS IsDescending,
                ic.key_ordinal AS Position
            FROM sys.indexes i
            INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
            WHERE i.object_id = OBJECT_ID(@SchemaTable)
                AND i.type > 0  -- Exclui HEAP
            ORDER BY i.name, ic.key_ordinal";

        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@SchemaTable", $"{schema}.{tableName}");

        await using var reader = await cmd.ExecuteReaderAsync();
        
        IndexInfo? currentIndex = null;
        
        while (await reader.ReadAsync())
        {
            var indexName = reader.GetString(0);
            
            if (currentIndex == null || currentIndex.Name != indexName)
            {
                if (currentIndex != null)
                {
                    indexes.Add(currentIndex);
                }

                currentIndex = new IndexInfo
                {
                    Name = indexName,
                    IsUnique = reader.GetBoolean(1),
                    IsPrimaryKey = reader.GetBoolean(2),
                    IsClustered = reader.GetString(3) == "CLUSTERED"
                };
            }

                currentIndex.Columns.Add(new IndexColumnInfo
                {
                    ColumnName = reader.GetString(4),
                    IsDescending = reader.GetBoolean(5),
                    Position = Convert.ToInt32(reader.GetValue(6))
                });
        }

        if (currentIndex != null)
        {
            indexes.Add(currentIndex);
        }

        return indexes;
    }

    public async Task<DataTable> GetTableDataAsync(string schema, string tableName)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT * FROM [{schema}].[{tableName}]";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var dataTable = new DataTable();
        dataTable.Load(reader);

        return dataTable;
    }

    public async IAsyncEnumerable<object[]> StreamTableDataAsync(string schema, string tableName)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var sql = $"SELECT * FROM [{schema}].[{tableName}]";
        await using var cmd = new SqlCommand(sql, connection);
        await using var reader = await cmd.ExecuteReaderAsync();

        var fieldCount = reader.FieldCount;

        while (await reader.ReadAsync())
        {
            var values = new object[fieldCount];
            reader.GetValues(values);
            yield return values;
        }
    }
}