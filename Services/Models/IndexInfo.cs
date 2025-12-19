using System.Collections.Generic;

namespace FluentMigratorWrapper.Services.DatabaseIntrospection.Models;

/// <summary>
/// Representa informações de um índice
/// </summary>
public class IndexInfo
{
   /// <summary>
   /// Nome do índice
   /// </summary>
   public string Name { get; set; } = string.Empty;

   /// <summary>
   /// Lista de colunas que compõem o índice
   /// </summary>
   public List<IndexColumnInfo> Columns { get; set; } = new();

   /// <summary>
   /// Indica se o índice é único (UNIQUE)
   /// </summary>
   public bool IsUnique { get; set; }

   /// <summary>
   /// Indica se o índice é a chave primária
   /// </summary>
   public bool IsPrimaryKey { get; set; }

   /// <summary>
   /// Indica se o índice é clustered
   /// </summary>
   public bool IsClustered { get; set; }
}

/// <summary>
/// Representa uma coluna dentro de um índice
/// </summary>
public class IndexColumnInfo
{
   /// <summary>
   /// Nome da coluna
   /// </summary>
   public string ColumnName { get; set; } = string.Empty;

   /// <summary>
   /// Indica se a coluna está em ordem descendente no índice
   /// </summary>
   public bool IsDescending { get; set; }

   /// <summary>
   /// Posição da coluna no índice (começa em 1)
   /// </summary>
   public int Position { get; set; }
}