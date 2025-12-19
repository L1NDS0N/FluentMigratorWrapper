using System.Data;
using System.Threading.Tasks;
using FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

namespace FluentMigratorWrapper.Services.DatabaseIntrospection;

/// <summary>
/// Interface para introspecção de banco de dados
/// </summary>
public interface IDatabaseIntrospector
{
    /// <summary>
    /// Obtém a estrutura completa do banco de dados
    /// </summary>
    /// <param name="schema">Schema do banco (ex: dbo)</param>
    /// <param name="specificTables">Tabelas específicas para obter (null = todas)</param>
    /// <returns>Informações da estrutura do banco</returns>
    Task<DatabaseInfo> GetDatabaseStructureAsync(string schema, string[]? specificTables = null);

    /// <summary>
    /// Obtém os dados de uma tabela específica
    /// </summary>
    /// <param name="schema">Schema da tabela</param>
    /// <param name="tableName">Nome da tabela</param>
    /// <returns>DataTable com os dados</returns>
    Task<DataTable> GetTableDataAsync(string schema, string tableName);
}