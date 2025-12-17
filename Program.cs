using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using FluentMigrator.Infrastructure;

namespace FluentMigratorWrapper
{

    /// <summary>
    /// An explicit migration source that exposes only the discovered migrations to the
    /// FluentMigrator runner. Returning only these prevents FluentMigrator from
    /// performing wide reflection over the application assembly and improves
    /// startup robustness when some types may fail to load.
    /// </summary>
    public sealed class ExplicitMigrationSource : IFilteringMigrationSource
    {
        private readonly IEnumerable<Type> _migrationTypes;

        public ExplicitMigrationSource(IEnumerable<Type> migrationTypes)
        {
            _migrationTypes = migrationTypes ?? Enumerable.Empty<Type>();
        }

        // The FluentMigrator IFilteringMigrationSource contract expects
        // migrations as instances of IMigration (not Type), so return that.
        /// <inheritdoc />
        public IEnumerable<IMigration> GetMigrations(Func<Type, bool> predicate)
        {
            var types = _migrationTypes;

            if (predicate != null)
                types = types.Where(predicate);

            // Create instances eagerly and return as an array to avoid
            // deferred execution causing AssemblyLoadContext lifetime issues.
            return types.Distinct().Select(t => (IMigration)Activator.CreateInstance(t)!).ToArray();
        }


        /// <summary>
        /// A lightweight IMigrationInfo implementation that wraps a concrete
        /// migration type and reads the <see cref="MigrationAttribute"/> values.
        /// </summary>
        private class MigrationInfo : IMigrationInfo
        {
            private readonly Type _migrationType;
            private readonly MigrationAttribute _attribute;

            public MigrationInfo(Type migrationType)
            {
                _migrationType = migrationType;

                _attribute = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
                    .FirstOrDefault() as MigrationAttribute
                    ?? throw new InvalidOperationException($"Migration {migrationType.Name} não possui atributo [Migration]");

                Version = _attribute.Version;
                TransactionBehavior = _attribute.TransactionBehavior;
                Description = _attribute.Description;
            }

            public long Version { get; }
            public TransactionBehavior TransactionBehavior { get; }
            public string? Description { get; }
            public bool IsBreakingChange => _attribute.BreakingChange;

            // IMigrationInfo.Migration expects an IMigration instance
            /// <inheritdoc />
            public IMigration Migration => (IMigration)Activator.CreateInstance(_migrationType)!;

            public IMigration GetMigration()
            {
                return (IMigration)Activator.CreateInstance(_migrationType)!;
            }

            public string GetName()
            {
                return _migrationType.Name;
            }

            public object Trait(string name)
            {
                return null!;
            }

            public bool HasTrait(string name)
            {
                return false;
            }

            public bool HasTags()
            {
                var tags = GetTagsArray();
                return tags != null && tags.Length > 0;
            }

            public bool HasTag(string tag)
            {
                var tags = GetTagsArray();
                return tags != null && tags.Contains(tag, StringComparer.OrdinalIgnoreCase);
            }

            public IEnumerable<string> GetTags()
            {
                var tags = GetTagsArray();
                return tags ?? Enumerable.Empty<string>();
            }

            private string[]? GetTagsArray()
            {
                // Some versions of FluentMigrator's MigrationAttribute may not expose a Tags property
                // so use reflection to attempt to read it, falling back to null.
                try
                {
                    var prop = _attribute.GetType().GetProperty("Tags");
                    if (prop != null)
                    {
                        var val = prop.GetValue(_attribute);
                        return val as string[];
                    }
                }
                catch { }

                return null;
            }
        }
    }

    public class MigrationConfig
    {
        [JsonPropertyName("connectionString")]
        public string? ConnectionString { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; } = "SqlServer";

        [JsonPropertyName("assembly")]
        public string? Assembly { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("autoBuild")]
        public bool AutoBuild { get; set; } = true;

        [JsonPropertyName("buildConfiguration")]
        public string BuildConfiguration { get; set; } = "Debug";

        [JsonPropertyName("namespace")]
        public string? Namespace { get; set; }

        [JsonPropertyName("nestedNamespaces")]
        public bool NestedNamespaces { get; set; } = false;

        [JsonPropertyName("transactionMode")]
        public string TransactionMode { get; set; } = "Session";

        [JsonPropertyName("commandTimeout")]
        public int CommandTimeout { get; set; } = 30;

        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }

        [JsonPropertyName("profile")]
        public string? Profile { get; set; }

        [JsonPropertyName("allowBreakingChange")]
        public bool AllowBreakingChange { get; set; } = false;

        [JsonPropertyName("previewOnly")]
        public bool PreviewOnly { get; set; } = false;

        [JsonPropertyName("showSql")]
        public bool ShowSql { get; set; } = true;

        [JsonPropertyName("showElapsedTime")]
        public bool ShowElapsedTime { get; set; } = true;

        [JsonPropertyName("workingDirectory")]
        public string? WorkingDirectory { get; set; }

        [JsonPropertyName("migrationsFolder")]
        public string? MigrationsFolder { get; set; }

        [JsonPropertyName("verbose")]
        public bool Verbose { get; set; } = false;
    }

    // Scanner que encontra migrations SEM carregar tipos desnecessários
    public class MigrationAssemblyScanner
    {
        public static Type[] GetMigrationTypes(Assembly assembly, string? namespaceFilter, bool nestedNamespaces, bool verbose)
        {
            var migrationTypes = new List<Type>();

            try
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("   Tentando GetExportedTypes()...");
                    Console.ResetColor();
                }

                // Primeira tentativa: GetExportedTypes (mais seguro, só tipos públicos)
                var exportedTypes = assembly.GetExportedTypes();

                foreach (var type in exportedTypes)
                {
                    try
                    {
                        if (IsMigrationType(type, namespaceFilter, nestedNamespaces))
                        {
                            migrationTypes.Add(type);
                            if (verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"   ✓ {type.FullName}");
                                Console.ResetColor();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"   ⚠ Ignorando tipo: {ex.Message}");
                            Console.ResetColor();
                        }
                        continue;
                    }
                }

                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   {migrationTypes.Count} migration(s) encontrada(s) via GetExportedTypes");
                    Console.ResetColor();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("   ⚠ GetExportedTypes falhou, tentando tipos carregados...");
                    Console.ResetColor();
                }

                // Segunda tentativa: pega apenas tipos que carregaram
                var loadedTypes = ex.Types.Where(t => t != null).ToArray();

                foreach (var type in loadedTypes)
                {
                    try
                    {
                        if (type != null && IsMigrationType(type, namespaceFilter, nestedNamespaces))
                        {
                            migrationTypes.Add(type);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"   ⚠ Erro ao escanear: {ex.Message}");
                    Console.ResetColor();
                }

                // Última tentativa: GetTypes() com tratamento de erro
                try
                {
                    var allTypes = assembly.GetTypes();
                    foreach (var type in allTypes)
                    {
                        try
                        {
                            if (IsMigrationType(type, namespaceFilter, nestedNamespaces))
                            {
                                migrationTypes.Add(type);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex2)
                {
                    var loadedTypes = ex2.Types.Where(t => t != null).ToArray();
                    foreach (var type in loadedTypes)
                    {
                        try
                        {
                            if (type != null && IsMigrationType(type, namespaceFilter, nestedNamespaces))
                            {
                                migrationTypes.Add(type);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            return migrationTypes.Distinct().ToArray();
        }

        private static bool IsMigrationType(Type type, string? namespaceFilter, bool nestedNamespaces)
        {
            try
            {
                // Verifica básico
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    return false;

                // Verifica se herda de Migration
                if (!typeof(Migration).IsAssignableFrom(type))
                    return false;

                // Verifica atributo [Migration]
                var hasAttribute = type.GetCustomAttributes(typeof(MigrationAttribute), false).Any();
                if (!hasAttribute)
                    return false;

                // Filtra namespace
                if (!string.IsNullOrEmpty(namespaceFilter))
                {
                    if (type.Namespace == null)
                        return false;

                    if (nestedNamespaces)
                    {
                        if (!type.Namespace.StartsWith(namespaceFilter, StringComparison.Ordinal))
                            return false;
                    }
                    else
                    {
                        if (!type.Namespace.Equals(namespaceFilter, StringComparison.Ordinal))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public class Program
    {
        private const string DefaultConfigFile = "fm.config.json";
        private static AssemblyLoadContext? _loadContext;
        // Controls verbose/debug logging after config is loaded
        private static bool _verbose;

        private static void LogInfo(string message)
        {
            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{ts}] INFO: {message}");
            Console.ResetColor();
        }

        private static void LogDebug(string message)
        {
            if (!_verbose) return;
            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[{ts}] DEBUG: {message}");
            Console.ResetColor();
        }

        private static void LogWarn(string message)
        {
            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{ts}] WARN: {message}");
            Console.ResetColor();
        }

        private static void LogError(string message)
        {
            var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{ts}] ERROR: {message}");
            Console.ResetColor();
        }

        static int Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    var firstArg = args[0].ToLower();

                    if (firstArg == "init" || firstArg == "--init")
                    {
                        CreateDefaultConfig();
                        return 0;
                    }

                    if (firstArg == "help" || firstArg == "--help" || firstArg == "-h")
                    {
                        ShowHelp();
                        return 0;
                    }

                    if (firstArg == "--version" || firstArg == "-v")
                    {
                        ShowVersion();
                        return 0;
                    }
                }

                var configFile = GetConfigFile(args, out var remainingArgs);

                if (!File.Exists(configFile))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  '{configFile}' não encontrado.");
                    Console.WriteLine("   Use 'fm-wrapper init'");
                    Console.ResetColor();
                    return 1;
                }

                var config = LoadConfig(configFile);

                if (!ValidateConfig(config))
                    return 1;

                // Enable verbose logging for later helper methods
                _verbose = config.Verbose;

                if (config.Verbose)
                {
                    LogInfo("Configuração:");
                    LogDebug($"Arquivo: {configFile}");
                    LogDebug($"Provider: {config.Provider}");
                    LogDebug($"Namespace: {config.Namespace ?? "(todos)"}");
                    LogDebug($"AutoBuild: {config.AutoBuild}");
                    Console.WriteLine();
                }

                if (!string.IsNullOrEmpty(config.WorkingDirectory) && Directory.Exists(config.WorkingDirectory))
                {
                    Directory.SetCurrentDirectory(config.WorkingDirectory);
                    Console.WriteLine($"📂 Diretório: {config.WorkingDirectory}");
                }

                var projectFile = GetProjectFile(config);
                if (projectFile == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("❌ Nenhum .csproj encontrado!");
                    Console.ResetColor();
                    return 1;
                }

                Console.WriteLine($"📁 Projeto: {Path.GetFileName(projectFile)}");

                if (config.AutoBuild)
                {
                    Console.WriteLine($"🔨 Compilando ({config.BuildConfiguration})...");

                    var buildResult = BuildProject(projectFile, config.BuildConfiguration);
                    if (buildResult != 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("❌ Build falhou!");
                        Console.ResetColor();
                        return buildResult;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("✅ Build OK!");
                    Console.ResetColor();
                    Console.WriteLine();
                }

                var assemblyPath = GetAssemblyPath(projectFile, config);

                if (!File.Exists(assemblyPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"❌ Assembly não encontrado: {assemblyPath}");
                    Console.ResetColor();
                    return 1;
                }

                Console.WriteLine($"📦 Assembly: {Path.GetFileName(assemblyPath)}");

                var assembly = LoadAssemblyWithDependencies(assemblyPath);

                Console.WriteLine("🔍 Buscando migrations...");
                var migrationTypes = MigrationAssemblyScanner.GetMigrationTypes(
                    assembly,
                    config.Namespace,
                    config.NestedNamespaces,
                    config.Verbose
                );

                if (migrationTypes.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("⚠️  Nenhuma migration encontrada!");
                    Console.WriteLine("   Verifique:");
                    Console.WriteLine("   - Herdam de FluentMigrator.Migration");
                    Console.WriteLine("   - Possuem [Migration(version)]");
                    if (!string.IsNullOrEmpty(config.Namespace))
                    {
                        Console.WriteLine($"   - Namespace: {config.Namespace}");
                    }
                    Console.ResetColor();
                    return 1;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ {migrationTypes.Length} migration(s)");
                Console.ResetColor();

                if (config.Verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    foreach (var type in migrationTypes)
                    {
                        var attr = type.GetCustomAttributes(typeof(MigrationAttribute), false)
                            .FirstOrDefault() as MigrationAttribute;
                        Console.WriteLine($"   {attr?.Version}: {type.Name}");
                    }
                    Console.ResetColor();
                }

                Console.WriteLine();

                var command = remainingArgs.Length > 0 ? remainingArgs[0].ToLower() : "migrate";
                var commandArgs = remainingArgs.Skip(1).ToArray();

                return ExecuteCommand(command, commandArgs, config, assembly, migrationTypes);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Erro: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Detalhes: {ex.InnerException.Message}");
                }
                Console.ResetColor();

                #if DEBUG
                Console.WriteLine();
                Console.WriteLine(ex.StackTrace);
                #endif

                return 1;
            }
            finally
            {
                _loadContext?.Unload();
            }
        }

        private static string GetConfigFile(string[] args, out string[] remainingArgs)
        {
            var configArg = args.FirstOrDefault(a => a.StartsWith("--config="));

            if (configArg != null)
            {
                var configFile = configArg.Substring("--config=".Length);
                remainingArgs = args.Where(a => a != configArg).ToArray();
                return configFile;
            }

            var configIndex = Array.IndexOf(args, "--config");
            if (configIndex >= 0 && configIndex + 1 < args.Length)
            {
                var configFile = args[configIndex + 1];
                remainingArgs = args.Where((a, i) => i != configIndex && i != configIndex + 1).ToArray();
                return configFile;
            }

            remainingArgs = args;
            return DefaultConfigFile;
        }

        private static void CreateDefaultConfig()
        {
            if (File.Exists(DefaultConfigFile))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  '{DefaultConfigFile}' já existe!");
                Console.Write("   Sobrescrever? (s/N): ");
                var response = Console.ReadLine()?.ToLower();
                Console.ResetColor();

                if (response != "s" && response != "sim")
                {
                    Console.WriteLine("Cancelado.");
                    return;
                }
            }

            var config = new MigrationConfig
            {
                ConnectionString = "Server=localhost;Database=MyDb;User Id=sa;Password=Pass123;TrustServerCertificate=True;",
                Provider = "SqlServer",
                AutoBuild = true,
                BuildConfiguration = "Debug",
                Namespace = "YourApi.Migrations",
                MigrationsFolder = "Migrations",
                ShowSql = true,
                Verbose = false
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(DefaultConfigFile, json);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✅ '{DefaultConfigFile}' criado!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("📝 Próximos passos:");
            Console.WriteLine("   1. Configure connectionString");
            Console.WriteLine("   2. Configure namespace (ex: YourApi.Migrations)");
            Console.WriteLine("   3. Execute: fm-wrapper migrate");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("FluentMigrator Wrapper v1.0.0");
            Console.WriteLine();
            Console.WriteLine("USO: fm-wrapper [comando] [opções]");
            Console.WriteLine();
            Console.WriteLine("COMANDOS:");
            Console.WriteLine("  init                  Cria fm.config.json");
            Console.WriteLine("  migrate               Aplica todas migrations");
            Console.WriteLine("  migrate:up [N]        Sobe N migrations");
            Console.WriteLine("  migrate:down [N]      Desce N migrations");
            Console.WriteLine("  rollback [VERSION]    Volta para versão");
            Console.WriteLine("  rollback:all          Desfaz tudo");
            Console.WriteLine("  list                  Lista migrations");
            Console.WriteLine("  validate              Valida versões");
            Console.WriteLine();
            Console.WriteLine("OPÇÕES:");
            Console.WriteLine("  --config FILE         Config customizado");
            Console.WriteLine("  --preview             Apenas visualizar");
            Console.WriteLine();
            Console.WriteLine("EXEMPLO:");
            Console.WriteLine("  fm-wrapper init");
            Console.WriteLine("  fm-wrapper migrate");
            Console.WriteLine("  fm-wrapper --config prod.json migrate");
        }

        private static void ShowVersion()
        {
            Console.WriteLine("FluentMigrator Wrapper v1.0.0");
        }

        private static bool ValidateConfig(MigrationConfig config)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("❌ connectionString não configurada!");
                Console.ResetColor();
                return false;
            }

            return true;
        }

        private static MigrationConfig LoadConfig(string configFile)
        {
            var json = File.ReadAllText(configFile);
            return JsonSerializer.Deserialize<MigrationConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? throw new InvalidOperationException($"Config inválida: {configFile}");
        }

        private static string? GetProjectFile(MigrationConfig config)
        {
            if (!string.IsNullOrEmpty(config.Project) && File.Exists(config.Project))
                return config.Project;

            var csprojFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");

            if (csprojFiles.Length == 0)
                return null;

            if (csprojFiles.Length > 1)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  Múltiplos projetos. Usando: {Path.GetFileName(csprojFiles[0])}");
                Console.ResetColor();
            }

            return csprojFiles[0];
        }

        private static string GetAssemblyPath(string projectFile, MigrationConfig config)
        {
            if (!string.IsNullOrEmpty(config.Assembly))
            {
                return Path.IsPathRooted(config.Assembly)
                    ? config.Assembly
                    : Path.Combine(Directory.GetCurrentDirectory(), config.Assembly);
            }

            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile) ?? Directory.GetCurrentDirectory();
            var targetFramework = GetTargetFramework(projectFile);

            return Path.Combine(projectDir, "bin", config.BuildConfiguration, targetFramework, $"{projectName}.dll");
        }

        private static string GetTargetFramework(string projectFile)
        {
            if (!File.Exists(projectFile))
                return "net8.0";

            var content = File.ReadAllText(projectFile);

            var match = System.Text.RegularExpressions.Regex.Match(content, @"<TargetFramework>(.*?)</TargetFramework>");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            match = System.Text.RegularExpressions.Regex.Match(content, @"<TargetFrameworks>(.*?)</TargetFrameworks>");
            if (match.Success)
                return match.Groups[1].Value.Split(';')[0].Trim();

            return "net8.0";
        }

        private static int BuildProject(string projectFile, string configuration)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectFile}\" -c {configuration} --nologo -v quiet",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };

            var errors = new System.Text.StringBuilder();

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    errors.AppendLine(e.Data);
            };

            process.Start();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode != 0 && errors.Length > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(errors.ToString());
                Console.ResetColor();
            }

            return process.ExitCode;
        }

        private static Assembly LoadAssemblyWithDependencies(string assemblyPath)
        {
            var fullPath = Path.GetFullPath(assemblyPath);
            var assemblyDirectory = Path.GetDirectoryName(fullPath)!;

            _loadContext = new AssemblyLoadContext("MigrationContext", isCollectible: true);

            _loadContext.Resolving += (context, assemblyName) =>
            {
                try
                {
                    var paths = new[]
                    {
                        Path.Combine(assemblyDirectory, $"{assemblyName.Name}.dll"),
                        Path.Combine(assemblyDirectory, "runtimes", "win", "lib", "net8.0", $"{assemblyName.Name}.dll"),
                        Path.Combine(assemblyDirectory, "runtimes", "win-x64", "lib", "net8.0", $"{assemblyName.Name}.dll")
                    };

                    foreach (var path in paths)
                    {
                        if (File.Exists(path))
                            return context.LoadFromAssemblyPath(path);
                    }
                }
                catch { }

                return null;
            };

            return _loadContext.LoadFromAssemblyPath(fullPath);
        }

        private static int ExecuteCommand(string command, string[] args, MigrationConfig config, Assembly assembly, Type[] migrationTypes)
        {
            var serviceProvider = CreateServices(config, migrationTypes);

            using var scope = serviceProvider.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

            try
            {
                var preview = args.Contains("--preview") || config.PreviewOnly;

                switch (command)
                {
                    case "migrate":
                        if (preview)
                        {
                            Console.WriteLine("🔍 Preview:");
                            runner.ListMigrations();
                        }
                        else
                        {
                            Console.WriteLine("▶️  Executando migrations...");
                            runner.MigrateUp();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("✅ Concluído!");
                            Console.ResetColor();
                        }
                        break;

                    case "migrate:up":
                        var stepsUp = args.Length > 0 && int.TryParse(args[0], out var s) ? s : 1;
                        Console.WriteLine($"▶️  Subindo {stepsUp} migration(s)...");
                        runner.MigrateUp(stepsUp);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Concluído!");
                        Console.ResetColor();
                        break;

                    case "migrate:down":
                        var stepsDown = args.Length > 0 && int.TryParse(args[0], out s) ? s : 1;
                        Console.WriteLine($"▼  Descendo {stepsDown} migration(s)...");
                        runner.MigrateDown(stepsDown);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Concluído!");
                        Console.ResetColor();
                        break;

                    case "rollback":
                        if (args.Length == 0 || !long.TryParse(args[0], out var version))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("❌ Use: fm-wrapper rollback 202412150001");
                            Console.ResetColor();
                            return 1;
                        }
                        Console.WriteLine($"⏪ Rollback para {version}...");
                        runner.RollbackToVersion(version);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Concluído!");
                        Console.ResetColor();
                        break;

                    case "rollback:all":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write("⚠️  Rollback de TUDO? (s/N): ");
                        Console.ResetColor();
                        if (Console.ReadLine()?.ToLower() == "s")
                        {
                            Console.WriteLine("⏪ Executando rollback...");
                            runner.Rollback(int.MaxValue);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("✅ Concluído!");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine("Cancelado.");
                        }
                        break;

                    case "list":
                        Console.WriteLine("📋 Migrations:");
                        Console.WriteLine();
                        runner.ListMigrations();
                        break;

                    case "validate":
                        Console.WriteLine("🔍 Validando...");
                        runner.ValidateVersionOrder();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("✅ Todas válidas!");
                        Console.ResetColor();
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"❌ Comando desconhecido: {command}");
                        Console.WriteLine("   Use 'fm-wrapper help'");
                        Console.ResetColor();
                        return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Erro: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   {ex.InnerException.Message}");
                }
                Console.ResetColor();
                return 1;
            }
        }

        private static IServiceProvider CreateServices(MigrationConfig config, Type[] migrationTypes)
        {
            var services = new ServiceCollection();

            // CRÍTICO: Registra source customizado com APENAS as migrations encontradas
            // Isso impede o FluentMigrator de fazer reflection em outros tipos
            services.AddSingleton<IFilteringMigrationSource>(_ => new ExplicitMigrationSource(migrationTypes));

            services.AddFluentMigratorCore()
                .ConfigureRunner(rb =>
                {
                    ConfigureProvider(rb, config.Provider);
                    rb.WithGlobalConnectionString(config.ConnectionString!);
                    rb.WithGlobalCommandTimeout(TimeSpan.FromSeconds(config.CommandTimeout));
                })
                .AddLogging(lb => lb.AddFluentMigratorConsole())
                .Configure<RunnerOptions>(opt =>
                {
                    opt.TransactionPerSession = config.TransactionMode.ToLower() == "session";
                    opt.Tags = config.Tags;
                    opt.Profile = config.Profile;
                    opt.AllowBreakingChange = config.AllowBreakingChange;
                })
                .Configure<FluentMigratorLoggerOptions>(opt =>
                {
                    opt.ShowSql = config.ShowSql;
                    opt.ShowElapsedTime = config.ShowElapsedTime;
                });

            return services.BuildServiceProvider(false);
        }

        private static void ConfigureProvider(IMigrationRunnerBuilder builder, string provider)
        {
            switch (provider.ToLower())
            {
                case "sqlserver":
                case "sqlserver2016":
                    builder.AddSqlServer2016();
                    break;
                case "sqlserver2014":
                    builder.AddSqlServer2014();
                    break;
                case "sqlserver2012":
                    builder.AddSqlServer2012();
                    break;
                case "postgresql":
                case "postgres":
                    builder.AddPostgres();
                    break;
                case "mysql":
                case "mysql5":
                    builder.AddMySql5();
                    break;
                case "sqlite":
                    builder.AddSQLite();
                    break;
                case "oracle":
                    builder.AddOracle();
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"⚠️  Provider '{provider}' desconhecido. Usando SqlServer2016.");
                    Console.ResetColor();
                    builder.AddSqlServer2016();
                    break;
            }
        }
    }
}