using System.Threading.Tasks;
using FluentMigratorWrapper.Services.DatabaseIntrospection;
using FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

namespace FluentMigratorWrapper.Services.CodeGeneration;

/// <summary>
/// Interface para geração de código de migrations do FluentMigrator
/// </summary>
public interface IMigrationGenerator
{
    /// <summary>
    /// Gera uma migration para uma única tabela
    /// </summary>
    /// <param name="table">Informações da tabela</param>
    /// <param name="timestamp">Timestamp da migration (ex: 20250101120000)</param>
    /// <returns>Código C# da migration</returns>
    string GenerateTableMigration(TableInfo table, string timestamp);

    /// <summary>
    /// Gera uma única migration contendo todas as tabelas do banco
    /// </summary>
    /// <param name="dbInfo">Informações do banco de dados</param>
    /// <param name="timestamp">Timestamp da migration</param>
    /// <returns>Código C# da migration</returns>
    string GenerateSingleMigration(DatabaseInfo dbInfo, string timestamp);

    /// <summary>
    /// Gera uma migration com dados (seed) das tabelas
    /// </summary>
    /// <param name="dbInfo">Informações do banco de dados</param>
    /// <param name="timestamp">Timestamp da migration</param>
    /// <param name="introspector">Introspector para buscar os dados</param>
    /// <returns>Código C# da migration de dados</returns>
    Task<string> GenerateDataMigrationAsync(
        DatabaseInfo dbInfo, 
        string timestamp, 
        IDatabaseIntrospector introspector);
}