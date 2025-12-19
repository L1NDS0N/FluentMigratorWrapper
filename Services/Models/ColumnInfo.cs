namespace FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

/// <summary>
/// Representa informações de uma coluna de tabela
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Nome da coluna
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dado da coluna (ex: int, varchar, datetime)
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Indica se a coluna aceita valores nulos
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Indica se a coluna é chave primária
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Indica se a coluna é auto-incremento (IDENTITY)
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Tamanho máximo da coluna (para tipos string/binary)
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Precisão da coluna (para tipos numéricos)
    /// </summary>
    public int? Precision { get; set; }

    /// <summary>
    /// Escala da coluna (para tipos numéricos)
    /// </summary>
    public int? Scale { get; set; }

    /// <summary>
    /// Valor padrão da coluna (DEFAULT constraint)
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Descrição/comentário da coluna (extended property)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// ID da coluna na ordem da tabela
    /// </summary>
    public int ColumnId { get; set; }
}