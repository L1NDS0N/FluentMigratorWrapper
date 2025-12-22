using System.CommandLine;
using System.CommandLine.Invocation;
using FluentMigratorWrapper.Services.DatabaseIntrospection;
using System.Linq;
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
            new[] { "--single-file" },
            description: "Gera uma única migration com todas as tabelas")
        {
            Arity = System.CommandLine.ArgumentArity.Zero
        };

        var includeDataOption = new Option<bool>(
            new[] { "--include-data" },
            description: "Inclui dados existentes nas migrations")
        {
            Arity = System.CommandLine.ArgumentArity.Zero
        };

        var separateDataFilesOption = new Option<bool>(
            new[] { "--separate-files", "-sd" },
            description: "Gera arquivos de seed separados por tabela dentro de uma subpasta (ex: Migrations/seed)")
        {
            Arity = System.CommandLine.ArgumentArity.Zero
        };

        var seedFolderOption = new Option<string>(
            aliases: new[] { "--seed-folder" },
            description: "Nome da subpasta onde os arquivos de seed serão colocados (quando --separate-data-files estiver ativo)",
            getDefaultValue: () => "seed");

        scaffoldCommand.AddOption(outputOption);
        scaffoldCommand.AddOption(namespaceOption);
        scaffoldCommand.AddOption(tablesOption);
        scaffoldCommand.AddOption(schemaOption);
        scaffoldCommand.AddOption(singleFileOption);
        scaffoldCommand.AddOption(includeDataOption);
        scaffoldCommand.AddOption(separateDataFilesOption);
        scaffoldCommand.AddOption(seedFolderOption);

        var configOption = new Option<string>("--config", getDefaultValue: () => "fm.config.json");
        scaffoldCommand.AddOption(configOption);

        scaffoldCommand.SetHandler(async (InvocationContext ctx) =>
        {
            // Read raw parse results and presence of options for debugging
            var parse = ctx.ParseResult;
            var output = parse.GetValueForOption(outputOption);
            var ns = parse.GetValueForOption(namespaceOption);
            var tables = parse.GetValueForOption(tablesOption);
            var schema = parse.GetValueForOption(schemaOption);
            var singleFile = parse.GetValueForOption(singleFileOption);
            var includeData = parse.GetValueForOption(includeDataOption);
            var separateDataFiles = parse.GetValueForOption(separateDataFilesOption);
            var seedFolder = parse.GetValueForOption(seedFolderOption);
            var configFile = parse.GetValueForOption(configOption);

            var tokenVals = parse.Tokens.Select(t => t.Value).ToArray();
            Console.WriteLine($"[PARSE] tokens=[{string.Join(' ', tokenVals)}]");
            Console.WriteLine($"[PARSE] hasOption single-file={tokenVals.Contains("--single-file") || tokenVals.Contains("-sd")}, include-data={tokenVals.Contains("--include-data")}, separate-files={tokenVals.Contains("--separate-files") || tokenVals.Contains("--separate-data-files") || tokenVals.Contains("-sd")}");
            Console.WriteLine($"[PARSE] values: output={output}, namespace={ns}, schema={schema}, seedFolder={seedFolder}");
            Console.WriteLine($"[PARSE] bools: singleFile={singleFile}, includeData={includeData}, separateDataFiles={separateDataFiles}");

            var cfg = FluentMigratorWrapper.Program.LoadConfig(configFile);
            await ExecuteScaffoldAsync(output, ns, tables, schema, singleFile, includeData, separateDataFiles, seedFolder, cfg);
        });

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
        bool separateDataFiles,
        string seedFolder,
        MigrationConfig config)
    {
        Console.WriteLine("🔍 Iniciando scaffold do banco de dados...\n");

        // Debug: show parsed flag values
        if (string.IsNullOrEmpty(outputPath)) outputPath = "Migrations";
        if (string.IsNullOrEmpty(namespaceName)) namespaceName = "Migrations";
        if (string.IsNullOrEmpty(seedFolder)) seedFolder = "seed";
        
        Console.WriteLine($"[DEBUG] singleFile={singleFile}, includeData={includeData}, separateDataFiles={separateDataFiles}, seedFolder={seedFolder}");

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

                if (separateDataFiles)
                {
                    var seedPath = Path.Combine(outputPath, seedFolder);
                    Directory.CreateDirectory(seedPath);

                    Console.WriteLine($"📝 Gerando arquivos de seed por tabela em: {seedPath}");

                    foreach (var table in databaseInfo.Tables)
                    {
                        var ts = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                        await Task.Delay(10);
                        var fileName = $"{ts}_Seed_{table.Name}.cs";
                        var filePath = Path.Combine(seedPath, fileName);
                        await generator.GenerateTableDataToFileAsync(table, ts, introspector, filePath);
                    }

                    Console.WriteLine($"\n✅ Arquivos de seed criados em: {seedPath}\n");
                }
                else
                {
                    var dataFileName = $"{dataTimestamp}_SeedData.cs";
                    var dataFilePath = Path.Combine(outputPath, dataFileName);

                    // Generate data migration using streaming to avoid high memory usage
                    await generator.GenerateDataMigrationToFileAsync(databaseInfo, dataTimestamp, introspector, dataFilePath);
                    Console.WriteLine($"✅ Dados exportados: {dataFilePath}\n");
                }
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