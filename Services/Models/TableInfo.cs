using System.Collections.Generic;

namespace FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

/// <summary>
/// Representa informações de uma tabela do banco de dados
/// </summary>
public class TableInfo
{
    /// <summary>
    /// Nome da tabela
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Schema da tabela (ex: dbo)
    /// </summary>
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// Lista de colunas da tabela
    /// </summary>
    public List<ColumnInfo> Columns { get; set; } = new();

    /// <summary>
    /// Lista de chaves estrangeiras da tabela
    /// </summary>
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = new();

    /// <summary>
    /// Lista de índices da tabela
    /// </summary>
    public List<IndexInfo> Indexes { get; set; } = new();

    /// <summary>
    /// Descrição/comentário da tabela (extended property)
    /// </summary>
    public string? Description { get; set; }
}