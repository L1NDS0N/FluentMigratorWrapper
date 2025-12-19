namespace FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

/// <summary>
/// Representa informações de uma chave estrangeira (Foreign Key)
/// </summary>
public class ForeignKeyInfo
{
    /// <summary>
    /// Nome da constraint de chave estrangeira
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome da coluna na tabela de origem
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Nome da tabela referenciada
    /// </summary>
    public string ReferencedTable { get; set; } = string.Empty;

    /// <summary>
    /// Schema da tabela referenciada
    /// </summary>
    public string ReferencedSchema { get; set; } = string.Empty;

    /// <summary>
    /// Nome da coluna referenciada na tabela de destino
    /// </summary>
    public string ReferencedColumn { get; set; } = string.Empty;

    /// <summary>
    /// Ação ao deletar registro referenciado (NO ACTION, CASCADE, SET NULL, SET DEFAULT)
    /// </summary>
    public string OnDelete { get; set; } = "NO ACTION";

    /// <summary>
    /// Ação ao atualizar registro referenciado (NO ACTION, CASCADE, SET NULL, SET DEFAULT)
    /// </summary>
    public string OnUpdate { get; set; } = "NO ACTION";
}