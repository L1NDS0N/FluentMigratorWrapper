using System.Collections.Generic;

namespace FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

/// <summary>
/// Representa informações completas da estrutura do banco de dados
/// </summary>
public class DatabaseInfo
{
    /// <summary>
    /// Lista de tabelas do banco de dados
    /// </summary>
    public List<TableInfo> Tables { get; set; } = new();

    /// <summary>
    /// Schema principal do banco (ex: dbo)
    /// </summary>
    public string Schema { get; set; } = "dbo";
}