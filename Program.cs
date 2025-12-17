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
    /// <summary>Supported languages for the wrapper</summary>
    public enum Language
    {
        /// <summary>Portuguese (Brazil)</summary>
        PT_BR,
        /// <summary>English</summary>
        EN
    }

    /// <summary>Provides translations for log messages in multiple languages</summary>
    public static class MessageTranslator
    {
        private static readonly Dictionary<(Language, string), string> _translations = new()
        {
            // Configuration messages
            { (Language.EN, "config_not_found"), "Configuration file '{0}' not found. Use 'fm-wrapper init'" },
            { (Language.PT_BR, "config_not_found"), "Arquivo de configuração '{0}' não encontrado. Use 'fm-wrapper init'" },

            { (Language.EN, "config_info"), "Configuration:" },
            { (Language.PT_BR, "config_info"), "Configuração:" },

            { (Language.EN, "config_file"), "File: {0}" },
            { (Language.PT_BR, "config_file"), "Arquivo: {0}" },

            { (Language.EN, "config_provider"), "Provider: {0}" },
            { (Language.PT_BR, "config_provider"), "Provider: {0}" },

            { (Language.EN, "config_namespace"), "Namespace: {0}" },
            { (Language.PT_BR, "config_namespace"), "Namespace: {0}" },

            { (Language.EN, "config_autobuild"), "AutoBuild: {0}" },
            { (Language.PT_BR, "config_autobuild"), "AutoBuild: {0}" },

            { (Language.EN, "working_dir"), "📂 Directory: {0}" },
            { (Language.PT_BR, "working_dir"), "📂 Diretório: {0}" },

            { (Language.EN, "project_file"), "📁 Project: {0}" },
            { (Language.PT_BR, "project_file"), "📁 Projeto: {0}" },

            { (Language.EN, "building"), "🔨 Building ({0})..." },
            { (Language.PT_BR, "building"), "🔨 Compilando ({0})..." },

            { (Language.EN, "build_failed"), "❌ Build failed!" },
            { (Language.PT_BR, "build_failed"), "❌ Build falhou!" },

            { (Language.EN, "build_ok"), "✅ Build OK!" },
            { (Language.PT_BR, "build_ok"), "✅ Build OK!" },

            { (Language.EN, "no_csproj"), "❌ No .csproj found!" },
            { (Language.PT_BR, "no_csproj"), "❌ Nenhum .csproj encontrado!" },

            { (Language.EN, "assembly_not_found"), "❌ Assembly not found: {0}" },
            { (Language.PT_BR, "assembly_not_found"), "❌ Assembly não encontrado: {0}" },

            { (Language.EN, "assembly_file"), "📦 Assembly: {0}" },
            { (Language.PT_BR, "assembly_file"), "📦 Assembly: {0}" },

            { (Language.EN, "searching_migrations"), "🔍 Searching for migrations..." },
            { (Language.PT_BR, "searching_migrations"), "🔍 Buscando migrations..." },

            { (Language.EN, "no_migrations"), "⚠️  No migrations found! Check:\n   - Inherit from FluentMigrator.Migration\n   - Have [Migration(version)]\n{0}" },
            { (Language.PT_BR, "no_migrations"), "⚠️  Nenhuma migration encontrada! Verifique:\n   - Herdam de FluentMigrator.Migration\n   - Possuem [Migration(version)]\n{0}" },

            { (Language.EN, "namespace_filter"), "   - Namespace: {0}" },
            { (Language.PT_BR, "namespace_filter"), "   - Namespace: {0}" },

            { (Language.EN, "migrations_found"), "✓ {0} migration(s)" },
            { (Language.PT_BR, "migrations_found"), "✓ {0} migration(s)" },

            // Command messages
            { (Language.EN, "preview_mode"), "🔍 Preview:" },
            { (Language.PT_BR, "preview_mode"), "🔍 Preview:" },

            { (Language.EN, "running_migrations"), "▶️  Running migrations..." },
            { (Language.PT_BR, "running_migrations"), "▶️  Executando migrations..." },

            { (Language.EN, "completed"), "✅ Completed!" },
            { (Language.PT_BR, "completed"), "✅ Concluído!" },

            { (Language.EN, "migrating_up"), "▶️  Migrating up {0} step(s)..." },
            { (Language.PT_BR, "migrating_up"), "▶️  Subindo {0} migration(s)..." },

            { (Language.EN, "migrating_down"), "▼  Migrating down {0} step(s)..." },
            { (Language.PT_BR, "migrating_down"), "▼  Descendo {0} migration(s)..." },

            { (Language.EN, "rollback_to"), "⏪ Rolling back to {0}..." },
            { (Language.PT_BR, "rollback_to"), "⏪ Rollback para {0}..." },

            { (Language.EN, "rollback_invalid"), "❌ Use: fm-wrapper rollback 202412150001" },
            { (Language.PT_BR, "rollback_invalid"), "❌ Use: fm-wrapper rollback 202412150001" },

            { (Language.EN, "rollback_confirm"), "⚠️  Rollback EVERYTHING? (y/N): " },
            { (Language.PT_BR, "rollback_confirm"), "⚠️  Rollback de TUDO? (s/N): " },

            { (Language.EN, "rollback_all"), "⏪ Rolling back..." },
            { (Language.PT_BR, "rollback_all"), "⏪ Executando rollback..." },

            { (Language.EN, "cancelled"), "Cancelled." },
            { (Language.PT_BR, "cancelled"), "Cancelado." },

            { (Language.EN, "migrations_list"), "📋 Migrations:" },
            { (Language.PT_BR, "migrations_list"), "📋 Migrations:" },

            { (Language.EN, "validating"), "🔍 Validating..." },
            { (Language.PT_BR, "validating"), "🔍 Validando..." },

            { (Language.EN, "valid_versions"), "✅ All valid!" },
            { (Language.PT_BR, "valid_versions"), "✅ Todas válidas!" },

            { (Language.EN, "unknown_command"), "❌ Unknown command: {0}\n   Use 'fm-wrapper help'" },
            { (Language.PT_BR, "unknown_command"), "❌ Comando desconhecido: {0}\n   Use 'fm-wrapper help'" },

            { (Language.EN, "error"), "❌ Error: {0}" },
            { (Language.PT_BR, "error"), "❌ Erro: {0}" },

            { (Language.EN, "details"), "   Details: {0}" },
            { (Language.PT_BR, "details"), "   Detalhes: {0}" },

            // Init command
            { (Language.EN, "init_exists"), "⚠️  '{0}' already exists!" },
            { (Language.PT_BR, "init_exists"), "⚠️  '{0}' já existe!" },

            { (Language.EN, "init_overwrite"), "   Overwrite? (y/N): " },
            { (Language.PT_BR, "init_overwrite"), "   Sobrescrever? (s/N): " },

            { (Language.EN, "init_created"), "✅ '{0}' created!" },
            { (Language.PT_BR, "init_created"), "✅ '{0}' criado!" },

            { (Language.EN, "init_next_steps"), "📝 Next steps:" },
            { (Language.PT_BR, "init_next_steps"), "📝 Próximos passos:" },

            { (Language.EN, "init_step1"), "   1. Configure connectionString" },
            { (Language.PT_BR, "init_step1"), "   1. Configure connectionString" },

            { (Language.EN, "init_step2"), "   2. Configure namespace (ex: YourApi.Migrations)" },
            { (Language.PT_BR, "init_step2"), "   2. Configure namespace (ex: YourApi.Migrations)" },

            { (Language.EN, "init_step3"), "   3. Execute: fm-wrapper migrate" },
            { (Language.PT_BR, "init_step3"), "   3. Execute: fm-wrapper migrate" },

            { (Language.EN, "connectionstring_empty"), "❌ connectionString not configured!" },
            { (Language.PT_BR, "connectionstring_empty"), "❌ connectionString não configurada!" },

            // Help messages
            { (Language.EN, "help_title"), "FluentMigrator Wrapper v1.0.0" },
            { (Language.PT_BR, "help_title"), "FluentMigrator Wrapper v1.0.0" },

            { (Language.EN, "help_usage"), "USAGE: fm-wrapper [command] [options]" },
            { (Language.PT_BR, "help_usage"), "USO: fm-wrapper [comando] [opções]" },

            { (Language.EN, "help_commands"), "COMMANDS:" },
            { (Language.PT_BR, "help_commands"), "COMANDOS:" },

            { (Language.EN, "help_init"), "  init                  Creates fm.config.json" },
            { (Language.PT_BR, "help_init"), "  init                  Cria fm.config.json" },

            { (Language.EN, "help_migrate"), "  migrate               Applies all migrations" },
            { (Language.PT_BR, "help_migrate"), "  migrate               Aplica todas migrations" },

            { (Language.EN, "help_migrate_up"), "  migrate:up [N]        Migrate up N migrations" },
            { (Language.PT_BR, "help_migrate_up"), "  migrate:up [N]        Sobe N migrations" },

            { (Language.EN, "help_migrate_down"), "  migrate:down [N]      Migrate down N migrations" },
            { (Language.PT_BR, "help_migrate_down"), "  migrate:down [N]      Desce N migrations" },

            { (Language.EN, "help_rollback"), "  rollback [VERSION]    Rollback to version" },
            { (Language.PT_BR, "help_rollback"), "  rollback [VERSION]    Volta para versão" },

            { (Language.EN, "help_rollback_all"), "  rollback:all          Undo everything" },
            { (Language.PT_BR, "help_rollback_all"), "  rollback:all          Desfaz tudo" },

            { (Language.EN, "help_list"), "  list                  List migrations" },
            { (Language.PT_BR, "help_list"), "  list                  Lista migrations" },

            { (Language.EN, "help_validate"), "  validate              Validate versions" },
            { (Language.PT_BR, "help_validate"), "  validate              Valida versões" },

            { (Language.EN, "help_options"), "OPTIONS:" },
            { (Language.PT_BR, "help_options"), "OPÇÕES:" },

            { (Language.EN, "help_config"), "  --config FILE         Custom config" },
            { (Language.PT_BR, "help_config"), "  --config FILE         Config customizado" },

            { (Language.EN, "help_preview"), "  --preview             Preview only" },
            { (Language.PT_BR, "help_preview"), "  --preview             Apenas visualizar" },

            { (Language.EN, "help_example"), "EXAMPLE:" },
            { (Language.PT_BR, "help_example"), "EXEMPLO:" },

            { (Language.EN, "help_ex1"), "  fm-wrapper init" },
            { (Language.PT_BR, "help_ex1"), "  fm-wrapper init" },

            { (Language.EN, "help_ex2"), "  fm-wrapper migrate" },
            { (Language.PT_BR, "help_ex2"), "  fm-wrapper migrate" },

            { (Language.EN, "help_ex3"), "  fm-wrapper --config prod.json migrate" },
            { (Language.PT_BR, "help_ex3"), "  fm-wrapper --config prod.json migrate" },
        };

        public static string Translate(Language lang, string key, params object?[] args)
        {
            if (_translations.TryGetValue((lang, key), out var template))
            {
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            // Fallback to key if translation not found
            return key;
        }
    }

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

        [JsonPropertyName("language")]
        public string Language { get; set; } = "PT-BR";
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
        private static Language _language = Language.PT_BR;  // Default to Portuguese

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

                // Parse language from config
                _language = config.Language.ToUpper() switch
                {
                    "EN" or "ENGLISH" => Language.EN,
                    _ => Language.PT_BR
                };

                // Enable verbose logging for later helper methods
                _verbose = config.Verbose;

                if (config.Verbose)
                {
                    LogInfo(MessageTranslator.Translate(_language, "config_info"));
                    LogDebug(MessageTranslator.Translate(_language, "config_file", configFile));
                    LogDebug(MessageTranslator.Translate(_language, "config_provider", config.Provider));
                    LogDebug(MessageTranslator.Translate(_language, "config_namespace", config.Namespace ?? "(all)"));
                    LogDebug(MessageTranslator.Translate(_language, "config_autobuild", config.AutoBuild));
                    Console.WriteLine();
                }

                if (!string.IsNullOrEmpty(config.WorkingDirectory) && Directory.Exists(config.WorkingDirectory))
                {
                    Directory.SetCurrentDirectory(config.WorkingDirectory);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(MessageTranslator.Translate(_language, "working_dir", config.WorkingDirectory));
                    Console.ResetColor();
                }

                var projectFile = GetProjectFile(config);
                if (projectFile == null)
                {
                    LogError(MessageTranslator.Translate(_language, "no_csproj"));
                    return 1;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(MessageTranslator.Translate(_language, "project_file", Path.GetFileName(projectFile)));
                Console.ResetColor();

                if (config.AutoBuild)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(MessageTranslator.Translate(_language, "building", config.BuildConfiguration));
                    Console.ResetColor();

                    var buildResult = BuildProject(projectFile, config.BuildConfiguration);
                    if (buildResult != 0)
                    {
                        LogError(MessageTranslator.Translate(_language, "build_failed"));
                        return buildResult;
                    }

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(MessageTranslator.Translate(_language, "build_ok"));
                    Console.ResetColor();
                    Console.WriteLine();
                }

                var assemblyPath = GetAssemblyPath(projectFile, config);

                if (!File.Exists(assemblyPath))
                {
                    LogError(MessageTranslator.Translate(_language, "assembly_not_found", assemblyPath));
                    return 1;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(MessageTranslator.Translate(_language, "assembly_file", Path.GetFileName(assemblyPath)));
                Console.ResetColor();

                var assembly = LoadAssemblyWithDependencies(assemblyPath);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(MessageTranslator.Translate(_language, "searching_migrations"));
                Console.ResetColor();

                var migrationTypes = MigrationAssemblyScanner.GetMigrationTypes(
                    assembly,
                    config.Namespace,
                    config.NestedNamespaces,
                    config.Verbose
                );

                if (migrationTypes.Length == 0)
                {
                    var nsFilter = !string.IsNullOrEmpty(config.Namespace) 
                        ? "\n" + MessageTranslator.Translate(_language, "namespace_filter", config.Namespace)
                        : "";
                    LogWarn(MessageTranslator.Translate(_language, "no_migrations", nsFilter));
                    return 1;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(MessageTranslator.Translate(_language, "migrations_found", migrationTypes.Length));
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
                LogError(MessageTranslator.Translate(_language, "error", ex.Message));
                if (ex.InnerException != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(MessageTranslator.Translate(_language, "details", ex.InnerException.Message));
                    Console.ResetColor();
                }

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
                Console.WriteLine(MessageTranslator.Translate(_language, "init_exists", DefaultConfigFile));
                Console.Write(MessageTranslator.Translate(_language, "init_overwrite"));
                var response = Console.ReadLine()?.ToLower();
                Console.ResetColor();

                var confirm = _language == Language.EN ? (response == "y" || response == "yes") : (response == "s" || response == "sim");
                if (!confirm)
                {
                    Console.WriteLine(MessageTranslator.Translate(_language, "cancelled"));
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
                Verbose = false,
                Language = _language == Language.EN ? "EN" : "PT-BR"
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            File.WriteAllText(DefaultConfigFile, json);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(MessageTranslator.Translate(_language, "init_created", DefaultConfigFile));
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(MessageTranslator.Translate(_language, "init_next_steps"));
            Console.ResetColor();
            Console.WriteLine(MessageTranslator.Translate(_language, "init_step1"));
            Console.WriteLine(MessageTranslator.Translate(_language, "init_step2"));
            Console.WriteLine(MessageTranslator.Translate(_language, "init_step3"));
        }

        private static void ShowHelp()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(MessageTranslator.Translate(_language, "help_title"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(_language, "help_usage"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(_language, "help_commands"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_init"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_migrate"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_migrate_up"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_migrate_down"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_rollback"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_rollback_all"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_list"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_validate"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(_language, "help_options"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_config"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_preview"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(_language, "help_example"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_ex1"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_ex2"));
            Console.WriteLine(MessageTranslator.Translate(_language, "help_ex3"));
            Console.ResetColor();
        }

        private static void ShowVersion()
        {
            Console.WriteLine("FluentMigrator Wrapper v1.0.0");
        }

        private static bool ValidateConfig(MigrationConfig config)
        {
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                LogError(MessageTranslator.Translate(_language, "connectionstring_empty"));
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
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(MessageTranslator.Translate(_language, "preview_mode"));
                            Console.ResetColor();
                            runner.ListMigrations();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(MessageTranslator.Translate(_language, "running_migrations"));
                            Console.ResetColor();
                            runner.MigrateUp();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(MessageTranslator.Translate(_language, "completed"));
                            Console.ResetColor();
                        }
                        break;

                    case "migrate:up":
                        var stepsUp = args.Length > 0 && int.TryParse(args[0], out var s) ? s : 1;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(_language, "migrating_up", stepsUp));
                        Console.ResetColor();
                        runner.MigrateUp(stepsUp);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(_language, "completed"));
                        Console.ResetColor();
                        break;

                    case "migrate:down":
                        var stepsDown = args.Length > 0 && int.TryParse(args[0], out s) ? s : 1;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(_language, "migrating_down", stepsDown));
                        Console.ResetColor();
                        runner.MigrateDown(stepsDown);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(_language, "completed"));
                        Console.ResetColor();
                        break;

                    case "rollback":
                        if (args.Length == 0 || !long.TryParse(args[0], out var version))
                        {
                            LogError(MessageTranslator.Translate(_language, "rollback_invalid"));
                            return 1;
                        }
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(_language, "rollback_to", version));
                        Console.ResetColor();
                        runner.RollbackToVersion(version);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(_language, "completed"));
                        Console.ResetColor();
                        break;

                    case "rollback:all":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(MessageTranslator.Translate(_language, "rollback_confirm"));
                        Console.ResetColor();
                        var response = Console.ReadLine()?.ToLower();
                        var confirm = _language == Language.EN ? response == "y" : response == "s";
                        if (confirm)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(MessageTranslator.Translate(_language, "rollback_all"));
                            Console.ResetColor();
                            runner.Rollback(int.MaxValue);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(MessageTranslator.Translate(_language, "completed"));
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine(MessageTranslator.Translate(_language, "cancelled"));
                        }
                        break;

                    case "list":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(_language, "migrations_list"));
                        Console.ResetColor();
                        Console.WriteLine();
                        runner.ListMigrations();
                        break;

                    case "validate":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(_language, "validating"));
                        Console.ResetColor();
                        runner.ValidateVersionOrder();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(_language, "valid_versions"));
                        Console.ResetColor();
                        break;

                    default:
                        LogError(MessageTranslator.Translate(_language, "unknown_command", command));
                        return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                LogError(MessageTranslator.Translate(_language, "error", ex.Message));
                if (ex.InnerException != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(MessageTranslator.Translate(_language, "details", ex.InnerException.Message));
                    Console.ResetColor();
                }
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