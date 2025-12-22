using System.Text;
using System.Data;
using System.Linq;
using System.IO;
using FluentMigratorWrapper.Services.DatabaseIntrospection;
using FluentMigratorWrapper.Services.DatabaseIntrospection.Models;
using System;
using System.Threading.Tasks;

namespace FluentMigratorWrapper.Services.CodeGeneration;


public class FluentMigratorCodeGenerator : IMigrationGenerator
{
    private readonly string _namespace;

    public FluentMigratorCodeGenerator(string namespaceName)
    {
        _namespace = namespaceName;
    }

    public string GenerateTableMigration(TableInfo table, string timestamp)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using FluentMigrator;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(table.Description))
        {
            sb.AppendLine($"/// <summary>");
            sb.AppendLine($"/// {table.Description}");
            sb.AppendLine($"/// </summary>");
        }

        sb.AppendLine($"[Migration({timestamp})]");
        sb.AppendLine($"public class Create{table.Name}Table : Migration");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Up()");
        sb.AppendLine("    {");
        sb.AppendLine($"        Create.Table(\"{table.Name}\")");

        // Gera colunas
        foreach (var column in table.Columns.OrderBy(c => c.ColumnId))
        {
            sb.Append(GenerateColumnDefinition(column, isLast: column == table.Columns.Last()));
        }

        sb.AppendLine(";");
        sb.AppendLine();

        // Gera FKs
        foreach (var fk in table.ForeignKeys)
        {
            sb.AppendLine(GenerateForeignKeyDefinition(table.Name, fk));
        }

        // Gera índices (exceto PK que já foi criada)
        foreach (var index in table.Indexes.Where(i => !i.IsPrimaryKey))
        {
            sb.AppendLine(GenerateIndexDefinition(table.Name, index));
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Drop Indexes");
        foreach (var index in table.Indexes.Where(i => !i.IsPrimaryKey))
        {
            sb.AppendLine($"        Delete.Index(\"{index.Name}\").OnTable(\"{table.Name}\");");
        }

        if (table.Indexes.Any(i => !i.IsPrimaryKey))
        {
            sb.AppendLine();
        }

        sb.AppendLine("        // Drop Foreign Keys");
        foreach (var fk in table.ForeignKeys)
        {
            sb.AppendLine($"        Delete.ForeignKey(\"{fk.Name}\").OnTable(\"{table.Name}\");");
        }

        if (table.ForeignKeys.Any())
        {
            sb.AppendLine();
        }

        sb.AppendLine("        // Drop Table");
        sb.AppendLine($"        Delete.Table(\"{table.Name}\");");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public string GenerateSingleMigration(DatabaseInfo dbInfo, string timestamp)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using FluentMigrator;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine($"[Migration({timestamp})]");
        sb.AppendLine("public class InitialSchema : Migration");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Up()");
        sb.AppendLine("    {");

        // Primeiro cria todas as tabelas (sem FKs)
        foreach (var table in dbInfo.Tables)
        {
            sb.AppendLine($"        // Tabela: {table.Name}");
            sb.AppendLine($"        Create.Table(\"{table.Name}\")");

            foreach (var column in table.Columns.OrderBy(c => c.ColumnId))
            {
                sb.Append(GenerateColumnDefinition(column, isLast: column == table.Columns.Last(), indent: 3));
            }

            sb.AppendLine(";");
            sb.AppendLine();
        }

        // Depois cria todas as FKs
        sb.AppendLine("        // Foreign Keys");
        foreach (var table in dbInfo.Tables)
        {
            foreach (var fk in table.ForeignKeys)
            {
                sb.AppendLine(GenerateForeignKeyDefinition(table.Name, fk, indent: 2));
            }
        }

        // Por fim, cria os índices
        sb.AppendLine();
        sb.AppendLine("        // Indexes");
        foreach (var table in dbInfo.Tables)
        {
            foreach (var index in table.Indexes.Where(i => !i.IsPrimaryKey))
            {
                sb.AppendLine(GenerateIndexDefinition(table.Name, index, indent: 2));
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down()");
        sb.AppendLine("    {");

        // Primeiro: Drop de índices (em ordem reversa)
        sb.AppendLine("        // Drop Indexes");
        for (int i = dbInfo.Tables.Count - 1; i >= 0; i--)
        {
            var table = dbInfo.Tables[i];
            foreach (var index in table.Indexes.Where(idx => !idx.IsPrimaryKey))
            {
                sb.AppendLine($"        Delete.Index(\"{index.Name}\").OnTable(\"{table.Name}\");");
            }
        }

        sb.AppendLine();

        // Segundo: Drop de Foreign Keys (em ordem reversa)
        sb.AppendLine("        // Drop Foreign Keys");
        for (int i = dbInfo.Tables.Count - 1; i >= 0; i--)
        {
            var table = dbInfo.Tables[i];
            foreach (var fk in table.ForeignKeys)
            {
                sb.AppendLine($"        Delete.ForeignKey(\"{fk.Name}\").OnTable(\"{table.Name}\");");
            }
        }

        sb.AppendLine();

        // Terceiro: Drop de tabelas em ordem reversa
        sb.AppendLine("        // Drop Tables");
        for (int i = dbInfo.Tables.Count - 1; i >= 0; i--)
        {
            sb.AppendLine($"        Delete.Table(\"{dbInfo.Tables[i].Name}\");");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    public async Task<string> GenerateDataMigrationAsync(DatabaseInfo dbInfo, string timestamp, IDatabaseIntrospector introspector)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using FluentMigrator;");
        sb.AppendLine();
        sb.AppendLine($"namespace {_namespace};");
        sb.AppendLine();
        sb.AppendLine($"[Migration({timestamp})]");
        sb.AppendLine("public class SeedData : Migration");
        sb.AppendLine("{");
        sb.AppendLine("    public override void Up()");
        sb.AppendLine("    {");

            foreach (var table in dbInfo.Tables)
            {
                var data = await introspector.GetTableDataAsync(table.Schema, table.Name);

                if (data.Rows.Count == 0)
                    continue;

                sb.AppendLine($"        // Dados da tabela {table.Name}");

                // Compact: criar um array de objetos e iterar com foreach para inserir
                sb.AppendLine($"        var rows{table.Name} = new[]");
                sb.AppendLine("        {");

                for (int r = 0; r < data.Rows.Count; r++)
                {
                    var row = data.Rows[r];
                    var assignments = new StringBuilder();
                    for (int i = 0; i < data.Columns.Count; i++)
                    {
                        var columnName = data.Columns[i].ColumnName;
                        var value = row[i];
                        if (i > 0) assignments.Append(", ");
                        assignments.Append($"{columnName} = {FormatValue(value)}");
                    }

                    var comma = r == data.Rows.Count - 1 ? "" : ",";
                    sb.AppendLine($"            new {{ {assignments} }}{comma}");
                }

                sb.AppendLine("        };\n");
                sb.AppendLine($"        foreach (var r in rows{table.Name})");
                sb.AppendLine("        {");
                sb.AppendLine($"            Insert.IntoTable(\"{table.Name}\").Row(r);");
                sb.AppendLine("        }");
                sb.AppendLine();
            }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public override void Down()");
        sb.AppendLine("    {");
        sb.AppendLine("        // Opcional: implementar remoção dos dados");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Gera a migration de dados diretamente em arquivo, usando leitura em streaming das linhas
    /// para reduzir uso de memória. Escreve a migration no caminho especificado.
    /// </summary>
    public async Task GenerateDataMigrationToFileAsync(DatabaseInfo dbInfo, string timestamp, IDatabaseIntrospector introspector, string filePath, int chunkSize = 500)
    {
        // Abre writer para escrita incremental
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var sw = new StreamWriter(fs, Encoding.UTF8);

        await sw.WriteLineAsync("using FluentMigrator;");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync($"namespace {_namespace};");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync($"[Migration({timestamp})]");
        await sw.WriteLineAsync("public class SeedData : Migration");
        await sw.WriteLineAsync("{");
        await sw.WriteLineAsync("    public override void Up()");
        await sw.WriteLineAsync("    {");

        foreach (var table in dbInfo.Tables)
        {
            // Use stream API to avoid loading whole table
            await sw.WriteLineAsync($"        // Dados da tabela {table.Name}");

            var columns = table.Columns.OrderBy(c => c.ColumnId).ToArray();

            var rowInChunk = 0;
            var chunkId = 0;
            var chunkSb = new StringBuilder();

            await foreach (var values in introspector.StreamTableDataAsync(table.Schema, table.Name))
            {
                if (columns.Length == 0) break; // nothing to write

                if (rowInChunk == 0)
                {
                    chunkSb.AppendLine($"        var data{table.Name}_{chunkId} = new[]");
                    chunkSb.AppendLine("        {");
                }

                var assignments = new StringBuilder();
                for (int i = 0; i < columns.Length; i++)
                {
                    var col = columns[i];
                    var value = i < values.Length ? values[i] : null;
                    if (i > 0) assignments.Append(", ");
                    assignments.Append($"{col.Name} = {FormatValue(value)}");
                }

                chunkSb.AppendLine($"            new {{ {assignments} }},");

                rowInChunk++;

                if (rowInChunk >= chunkSize)
                {
                    chunkSb.AppendLine("        };\n");
                    chunkSb.AppendLine($"        foreach (var r in data{table.Name}_{chunkId})");
                    chunkSb.AppendLine("        {");
                    chunkSb.AppendLine($"            Insert.IntoTable(\"{table.Name}\").Row(r);");
                    chunkSb.AppendLine("        }\n");

                    await sw.WriteAsync(chunkSb.ToString());
                    chunkSb.Clear();
                    rowInChunk = 0;
                    chunkId++;
                }
            }

            if (rowInChunk > 0)
            {
                chunkSb.AppendLine("        };\n");
                chunkSb.AppendLine($"        foreach (var r in data{table.Name}_{chunkId})");
                chunkSb.AppendLine("        {");
                chunkSb.AppendLine($"            Insert.IntoTable(\"{table.Name}\").Row(r);");
                chunkSb.AppendLine("        }\n");

                await sw.WriteAsync(chunkSb.ToString());
                chunkSb.Clear();
            }

            await sw.WriteLineAsync();
        }

        await sw.WriteLineAsync("    }");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync("    public override void Down()");
        await sw.WriteLineAsync("    {");
        await sw.WriteLineAsync("        // Opcional: implementar remoção dos dados");
        await sw.WriteLineAsync("    }");
        await sw.WriteLineAsync("}");

        await sw.FlushAsync();
    }

    public async Task GenerateTableDataToFileAsync(TableInfo table, string timestamp, IDatabaseIntrospector introspector, string filePath, int chunkSize = 500)
    {
        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var sw = new StreamWriter(fs, Encoding.UTF8);

        await sw.WriteLineAsync("using FluentMigrator;");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync($"namespace {_namespace};");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync($"[Migration({timestamp})]");
        await sw.WriteLineAsync($"public class Seed_{table.Name} : Migration");
        await sw.WriteLineAsync("{");
        await sw.WriteLineAsync("    public override void Up()");
        await sw.WriteLineAsync("    {");

        await sw.WriteLineAsync($"        // Dados da tabela {table.Name}");

        var columns = table.Columns.OrderBy(c => c.ColumnId).ToArray();
        var rowIndex = 0;
        var chunkSb = new StringBuilder();

        var chunkRowCount = 0;
        await foreach (var values in introspector.StreamTableDataAsync(table.Schema, table.Name))
        {
            if (columns.Length == 0) break;

            if (chunkRowCount == 0)
            {
                chunkSb.Append($"        Insert.IntoTable(\"{table.Name}\")");
                chunkSb.AppendLine();
            }

            var assignments = new StringBuilder();
            for (int i = 0; i < columns.Length; i++)
            {
                var col = columns[i];
                var value = i < values.Length ? values[i] : null;
                if (i > 0) assignments.Append(", ");
                assignments.Append($"{col.Name} = {FormatValue(value)}");
            }

            chunkSb.AppendLine($"            .Row(new {{ {assignments} }})" );
            chunkSb.AppendLine();
            rowIndex++;
            chunkRowCount++;

            if (rowIndex % chunkSize == 0)
            {
                var txt = chunkSb.ToString().TrimEnd();
                if (!txt.EndsWith(";")) txt += ";";
                txt += Environment.NewLine + Environment.NewLine;
                await sw.WriteAsync(txt);
                chunkSb.Clear();
                chunkRowCount = 0;
            }
        }

        if (chunkSb.Length > 0)
        {
            var txt = chunkSb.ToString().TrimEnd();
            if (!txt.EndsWith(";")) txt += ";";
            txt += Environment.NewLine + Environment.NewLine;
            await sw.WriteAsync(txt);
            chunkSb.Clear();
        }

        await sw.WriteLineAsync();
        await sw.WriteLineAsync("    }");
        await sw.WriteLineAsync();
        await sw.WriteLineAsync("    public override void Down()");
        await sw.WriteLineAsync("    {");
        await sw.WriteLineAsync("        // Opcional: implementar remoção dos dados");
        await sw.WriteLineAsync("    }");
        await sw.WriteLineAsync("}");

        await sw.FlushAsync();
    }

    private string GenerateColumnDefinition(ColumnInfo column, bool isLast, int indent = 2)
    {
        var sb = new StringBuilder();
        var indentStr = new string(' ', indent * 4);

        sb.Append($"{indentStr}    .WithColumn(\"{column.Name}\")");

        // Tipo de dado
        sb.Append(column.DataType.ToLowerInvariant() switch
        {
            "int" => ".AsInt32()",
            "bigint" => ".AsInt64()",
            "smallint" => ".AsInt16()",
            "tinyint" => ".AsByte()",
            "bit" => ".AsBoolean()",
            "decimal" or "numeric" => $".AsDecimal({column.Precision ?? 18}, {column.Scale ?? 0})",
            "money" => ".AsCurrency()",
            "float" => ".AsDouble()",
            "real" => ".AsFloat()",
            "datetime" or "datetime2" => ".AsDateTime()",
            "date" => ".AsDate()",
            "time" => ".AsTime()",
            "datetimeoffset" => ".AsDateTimeOffset()",
            "uniqueidentifier" => ".AsGuid()",
            "varchar" => column.MaxLength.HasValue ? $".AsString({column.MaxLength})" : ".AsString()",
            "nvarchar" => column.MaxLength.HasValue ? $".AsString({column.MaxLength})" : ".AsString()",
            "char" => $".AsFixedLengthString({column.MaxLength ?? 1})",
            "nchar" => $".AsFixedLengthString({column.MaxLength ?? 1})",
            "text" or "ntext" => ".AsString(int.MaxValue)",
            "binary" or "varbinary" => column.MaxLength.HasValue ? $".AsBinary({column.MaxLength})" : ".AsBinary()",
            "xml" => ".AsXml()",
            _ => $".AsCustom(\"{column.DataType}\")"
        });

        // Nullable
        if (!column.IsNullable)
        {
            sb.Append(".NotNullable()");
        }
        else
        {
            sb.Append(".Nullable()");
        }

        // Primary Key
        if (column.IsPrimaryKey)
        {
            sb.Append(".PrimaryKey()");
        }

        // Identity
        if (column.IsIdentity)
        {
            sb.Append(".Identity()");
        }

        // Default Value
        if (!string.IsNullOrEmpty(column.DefaultValue))
        {
            var formattedDefault = FormatDefaultValue(column.DefaultValue, column.DataType);
            if (!string.IsNullOrEmpty(formattedDefault))
            {
                sb.Append($".WithDefaultValue({formattedDefault})");
            }
        }

        if (!isLast)
        {
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatDefaultValue(string defaultValue, string dataType)
    {
        if (string.IsNullOrWhiteSpace(defaultValue))
            return string.Empty;

        var cleaned = defaultValue.Trim();
        while (cleaned.StartsWith("(") && cleaned.EndsWith(")"))
        {
            cleaned = cleaned[1..^1].Trim();
        }

        var lowerDataType = dataType.ToLowerInvariant();

        if (lowerDataType == "uniqueidentifier")
        {

            var guidValue = cleaned.Trim('\'', '"');
            return $"\"{guidValue}\"";
        }

        if (lowerDataType == "bit")
        {
            if (cleaned.Contains("CONVERT") || cleaned.Contains("convert"))
            {

                if (cleaned.Contains("(1)"))
                    return "true";
                if (cleaned.Contains("(0)"))
                    return "false";
            }


            if (cleaned == "0" || cleaned.ToLower() == "false")
                return "false";
            if (cleaned == "1" || cleaned.ToLower() == "true")
                return "true";
        }

        if (lowerDataType.Contains("varchar") || lowerDataType.Contains("char") ||
            lowerDataType.Contains("text") || lowerDataType == "time")
        {

            if (cleaned.StartsWith("N'") || cleaned.StartsWith("n'"))
            {
                cleaned = cleaned[2..];
            }

            cleaned = cleaned.Trim('\'');


            if (string.IsNullOrEmpty(cleaned))
                return "\"\"";


            cleaned = cleaned.Replace("\"", "\\\"");
            return $"\"{cleaned}\"";
        }

        if (lowerDataType.Contains("int") || lowerDataType.Contains("decimal") ||
            lowerDataType.Contains("numeric") || lowerDataType.Contains("money") ||
            lowerDataType.Contains("float") || lowerDataType.Contains("real"))
        {

            if (cleaned.Contains("CONVERT") || cleaned.Contains("convert") ||
                cleaned.Contains("CAST") || cleaned.Contains("cast"))
            {

                var match = System.Text.RegularExpressions.Regex.Match(cleaned, @"[-+]?\d+\.?\d*");
                if (match.Success)
                    return match.Value;
            }


            if (decimal.TryParse(cleaned, out _))
                return cleaned;
        }

        if (lowerDataType.Contains("date") || lowerDataType.Contains("time"))
        {

            if (cleaned.ToUpper().Contains("GETDATE") ||
                cleaned.ToUpper().Contains("GETUTCDATE") ||
                cleaned.ToUpper().Contains("SYSDATETIME"))
            {
                return "SystemMethods.CurrentDateTime";
            }


            cleaned = cleaned.Trim('\'');
            if (!string.IsNullOrEmpty(cleaned) && DateTime.TryParse(cleaned, out var dt))
            {
                return $"\"{dt:yyyy-MM-dd HH:mm:ss}\"";
            }


            if (lowerDataType == "time")
            {
                return $"\"{cleaned}\"";
            }
        }

        if (cleaned.StartsWith("'") && cleaned.EndsWith("'"))
        {
            cleaned = cleaned[1..^1];
            return $"\"{cleaned}\"";
        }

        return cleaned;
    }

    private string GenerateForeignKeyDefinition(string tableName, ForeignKeyInfo fk, int indent = 2)
    {
        var indentStr = new string(' ', indent * 4);
        var onDelete = fk.OnDelete == "CASCADE" ? ".OnDelete(System.Data.Rule.Cascade)" : "";

        return $"{indentStr}Create.ForeignKey(\"{fk.Name}\")" +
               $"\n{indentStr}    .FromTable(\"{tableName}\").ForeignColumn(\"{fk.ColumnName}\")" +
               $"\n{indentStr}    .ToTable(\"{fk.ReferencedTable}\").PrimaryColumn(\"{fk.ReferencedColumn}\"){onDelete};";
    }

    private string GenerateIndexDefinition(string tableName, IndexInfo index, int indent = 2)
    {
        var indentStr = new string(' ', indent * 4);
        var sb = new StringBuilder();

        sb.Append($"{indentStr}Create.Index(\"{index.Name}\")");
        sb.Append($"\n{indentStr}    .OnTable(\"{tableName}\")");

        foreach (var col in index.Columns.OrderBy(c => c.Position))
        {
            var direction = col.IsDescending ? ".Descending()" : ".Ascending()";
            sb.Append($"\n{indentStr}    .OnColumn(\"{col.ColumnName}\"){direction}");
        }

        if (index.IsClustered)
        {
            sb.Append("\n" + indentStr + "    .WithOptions().Clustered()");
        }

        if (index.IsUnique)
        {
            sb.Append("\n" + indentStr + "    .WithOptions().Unique()");
        }

        sb.Append(";");

        return sb.ToString();
    }

    private string FormatValue(object value)
    {
        if (value == DBNull.Value || value == null)
            return "null";

        return value switch
        {
            string s => $"@\"{s.Replace("\"", "\"\"")}\"",
            DateTime dt => $"DateTime.Parse(\"{dt:yyyy-MM-dd HH:mm:ss}\")",
            bool b => b.ToString().ToLower(),
            Guid g => $"Guid.Parse(\"{g}\")",
            byte[] bytes => $"new byte[] {{ {string.Join(", ", bytes.Select(b => b.ToString()))} }}",
            _ => value.ToString() ?? "null"
        };
    }
}