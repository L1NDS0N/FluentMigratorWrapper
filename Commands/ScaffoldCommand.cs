using System.CommandLine;
using FluentMigratorWrapper.Services.DatabaseIntrospection;
using FluentMigratorWrapper.Services.CodeGeneration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FluentMigratorWrapper.Commands;

public class ScaffoldCommand
{
    public static Command Create()
    {
        var scaffoldCommand = new Command("scaffold", "Gera migrations do FluentMigrator a partir do banco de dados existente");

        var outputOption = new Option<string>(
            aliases: new[] { "--output", "-o" },
            description: "Diretório de saída para as migrations geradas",
            getDefaultValue: () => "Migrations");

        var namespaceOption = new Option<string>(
            aliases: new[] { "--namespace", "-n" },
            description: "Namespace para as migrations geradas",
            getDefaultValue: () => "Migrations");

        var tablesOption = new Option<string[]>(
            aliases: new[] { "--tables", "-t" },
            description: "Tabelas específicas para fazer scaffold (se não especificado, todas as tabelas serão incluídas)");

        var schemaOption = new Option<string>(
            aliases: new[] { "--schema", "-s" },
            description: "Schema do banco de dados",
            getDefaultValue: () => "dbo");

        var singleFileOption = new Option<bool>(
            aliases: new[] { "--single-file" },
            description: "Gera uma única migration com todas as tabelas",
            getDefaultValue: () => false);

        var includeDataOption = new Option<bool>(
            aliases: new[] { "--include-data" },
            description: "Inclui dados existentes nas migrations",
            getDefaultValue: () => false);

        scaffoldCommand.AddOption(outputOption);
        scaffoldCommand.AddOption(namespaceOption);
        scaffoldCommand.AddOption(tablesOption);
        scaffoldCommand.AddOption(schemaOption);
        scaffoldCommand.AddOption(singleFileOption);
        scaffoldCommand.AddOption(includeDataOption);

        scaffoldCommand.SetHandler(async (output, ns, tables, schema, singleFile, includeData, configFile) =>
        {
            // keep CLI handler compatibility: load config and call shared method
            var cfg = FluentMigratorWrapper.Program.LoadConfig(configFile);
            await ExecuteScaffoldAsync(output, ns, tables, schema, singleFile, includeData, cfg);
        }, outputOption, namespaceOption, tablesOption, schemaOption, singleFileOption, includeDataOption,
           new Option<string>("--config", getDefaultValue: () => "fm.config.json"));

        return scaffoldCommand;
    }

    // Public entry so other code (CommandExecutor) can call scaffold programmatically
        public static async Task ExecuteScaffoldAsync(
        string outputPath,
        string namespaceName,
        string[] tables,
        string schema,
        bool singleFile,
        bool includeData,
        MigrationConfig config)
    {
        Console.WriteLine("🔍 Iniciando scaffold do banco de dados...\n");

        // Cria o introspector baseado no provider
            IDatabaseIntrospector introspector = config.Provider.ToLowerInvariant() switch
            {
                "sqlserver" => new SqlServerIntrospector(config.ConnectionString ?? string.Empty),
                _ => throw new NotSupportedException($"Provider '{config.Provider}' não suportado para scaffold")
            };

            // Determine effective schema:
            // 1) If user provided --schema/-s, use it.
            // 2) Else try to parse common schema keys from the connection string.
            // 3) If not found, ask the introspector to detect the default schema for the connection user.
            // Prefer explicit config value if provided
            string effectiveSchema = !string.IsNullOrWhiteSpace(config.DefaultSchema)
                ? config.DefaultSchema!
                : schema;

            if (string.IsNullOrWhiteSpace(effectiveSchema) || effectiveSchema == "dbo")
            {
                // Try to parse from connection string
                string? parsed = ParseSchemaFromConnectionString(config.ConnectionString);
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    effectiveSchema = parsed!;
                }
                else
                {
                    if (introspector is SqlServerIntrospector sqlIntrospector)
                    {
                        try
                        {
                            var detected = await sqlIntrospector.DetectDefaultSchemaAsync();
                            if (!string.IsNullOrWhiteSpace(detected)) effectiveSchema = detected;
                        }
                        catch
                        {
                            // Fallback to dbo if detection fails
                            effectiveSchema = "dbo";
                        }
                    }
                }
            }

            // local helper: parse schema-like keys from connection string
            static string? ParseSchemaFromConnectionString(string? cs)
            {
                if (string.IsNullOrWhiteSpace(cs)) return null;
                var pairs = cs.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in pairs)
                {
                    var idx = p.IndexOf('=');
                    if (idx <= 0) continue;
                    var key = p.Substring(0, idx).Trim().ToLowerInvariant();
                    var val = p.Substring(idx + 1).Trim();
                    if (key == "current schema" || key == "currentschema" || key == "schema" || key == "searchpath" || key == "search path" || key == "default schema")
                        return val;
                }
                return null;
            }

            try
            {
                // Obtém informações do banco
                Console.WriteLine($"📊 Lendo estrutura do banco (schema: {effectiveSchema})...");
                var databaseInfo = await introspector.GetDatabaseStructureAsync(effectiveSchema, tables);

            Console.WriteLine($"✅ Encontradas {databaseInfo.Tables.Count} tabela(s)\n");

            // Cria o gerador de código
            var generator = new FluentMigratorCodeGenerator(namespaceName);

            // Garante que o diretório existe
            Directory.CreateDirectory(outputPath);

            if (singleFile)
            {
                // Gera uma única migration com todas as tabelas
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var fileName = $"{timestamp}_InitialSchema.cs";
                var filePath = Path.Combine(outputPath, fileName);

                Console.WriteLine($"📝 Gerando migration única: {fileName}");
                var code = generator.GenerateSingleMigration(databaseInfo, timestamp);
                await File.WriteAllTextAsync(filePath, code);
                Console.WriteLine($"✅ Arquivo criado: {filePath}\n");
            }
            else
            {
                // Gera uma migration por tabela
                foreach (var table in databaseInfo.Tables)
                {
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                    await Task.Delay(10); // Garante timestamps únicos
                    
                    var fileName = $"{timestamp}_Create{table.Name}Table.cs";
                    var filePath = Path.Combine(outputPath, fileName);

                    Console.WriteLine($"📝 Gerando migration: {fileName}");
                    var code = generator.GenerateTableMigration(table, timestamp);
                    await File.WriteAllTextAsync(filePath, code);
                }
                Console.WriteLine($"\n✅ {databaseInfo.Tables.Count} arquivo(s) criado(s) em: {outputPath}\n");
            }

            // Se solicitado, gera migrations com dados
            if (includeData)
            {
                Console.WriteLine("📊 Gerando migrations de dados...");
                var dataTimestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var dataFileName = $"{dataTimestamp}_SeedData.cs";
                var dataFilePath = Path.Combine(outputPath, dataFileName);

                var dataCode = await generator.GenerateDataMigrationAsync(databaseInfo, dataTimestamp, introspector);
                await File.WriteAllTextAsync(dataFilePath, dataCode);
                Console.WriteLine($"✅ Dados exportados: {dataFilePath}\n");
            }

            Console.WriteLine("🎉 Scaffold concluído com sucesso!");
            Console.WriteLine($"\n💡 Próximos passos:");
            Console.WriteLine($"   1. Revise as migrations geradas em: {outputPath}");
            Console.WriteLine($"   2. Execute: fm-wrapper migrate");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Erro durante scaffold: {ex.Message}");
            Console.ResetColor();
            Environment.Exit(1);
        }
    }

    // Removed local LoadConfiguration in favor of using Program.LoadConfig or receiving MigrationConfig directly
}