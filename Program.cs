using System;
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
using FluentMigrator.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using FluentMigratorWrapper.Commands;
using FluentMigratorWrapper.Services;

namespace FluentMigratorWrapper
{
    public class Program
    {
        private const string DefaultConfigFile = "fm.config.json";
        private static AssemblyLoadContext? _loadContext;
        private static bool _verbose;
        private static Language _language = Language.PT_BR;

        static int Main(string[] args)
        {
            try
            {
                var configFile = GetConfigFile(args, out var remainingArgs);

                // If the user asked for help or init, allow it before requiring a config file
                var commandArg = remainingArgs.Length > 0 ? remainingArgs[0].ToLower() : "migrate";
                if (commandArg == "help" || commandArg == "--help" || commandArg == "-h")
                {
                    // If a config file is present, respect its language setting for help output
                    if (File.Exists(configFile))
                    {
                        try
                        {
                            var cfg = LoadConfig(configFile);
                            _language = cfg.Language != null && cfg.Language.ToUpper() == "EN" ? Language.EN : Language.PT_BR;
                        }
                        catch
                        {
                            // Ignore config parsing errors here and fall back to default language
                            _language = Language.PT_BR;
                        }
                    }
                    return Commands.CommandExecutor.PrintHelp(_language);
                }

                if (commandArg == "init" || commandArg == "--init")
                {
                    CreateDefaultConfig();
                    return 0;
                }

                if (!File.Exists(configFile))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Config file '{configFile}' not found.");
                    Console.ResetColor();
                    return 1;
                }

                var config = LoadConfig(configFile);
                _language = config.Language.ToUpper() == "EN" ? Language.EN : Language.PT_BR;
                _verbose = config.Verbose;

                // If scaffold was requested, run it now before touching project/assembly/migrations
                var cmd = remainingArgs.Length > 0 ? remainingArgs[0].ToLower() : "migrate";
                var cmdArgs = remainingArgs.Skip(1).ToArray();
                if (cmd == "scaffold")
                {
                    return Commands.CommandExecutor.ExecuteScaffold(cmdArgs, config, _language);
                }

                var projectFile = GetProjectFile(config);
                if (projectFile == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("No .csproj found.");
                    Console.ResetColor();
                    return 1;
                }

                if (config.AutoBuild)
                {
                    var rc = BuildProject(projectFile, config.BuildConfiguration);
                    if (rc != 0) return rc;
                }

                var assemblyPath = GetAssemblyPath(projectFile, config);
                if (!File.Exists(assemblyPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Assembly not found: {assemblyPath}");
                    Console.ResetColor();
                    return 1;
                }

                var assembly = LoadAssemblyWithDependencies(assemblyPath);
                var migrationTypes = MigrationAssemblyScanner.GetMigrationTypes(assembly, config.Namespace, config.NestedNamespaces, config.Verbose);

                if (migrationTypes.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No migrations found.");
                    Console.ResetColor();
                    return 1;
                }

                var command = remainingArgs.Length > 0 ? remainingArgs[0].ToLower() : "migrate";
                var commandArgs = remainingArgs.Skip(1).ToArray();

                return CommandExecutor.ExecuteCommand(command, commandArgs, config, assembly, migrationTypes, _language);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
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
                remainingArgs = args.Where(a => a != configArg).ToArray();
                return configArg.Substring("--config=".Length);
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
                DefaultSchema = "dbo",
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

        internal static MigrationConfig LoadConfig(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<MigrationConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
        }

        private static string? GetProjectFile(MigrationConfig config)
        {
            if (!string.IsNullOrEmpty(config.Project) && File.Exists(config.Project)) return config.Project;
            var cs = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj");
            return cs.FirstOrDefault();
        }

        private static int BuildProject(string projectFile, string configuration)
        {
            using var p = Process.Start(new ProcessStartInfo("dotnet", $"build \"{projectFile}\" -c {configuration} --nologo") 
            { 
                RedirectStandardOutput = true, 
                RedirectStandardError = true, 
                UseShellExecute = false 
            });
            p!.WaitForExit();
            return p.ExitCode;
        }

        private static string GetAssemblyPath(string projectFile, MigrationConfig config)
        {
            if (!string.IsNullOrEmpty(config.Assembly))
                return Path.IsPathRooted(config.Assembly) ? config.Assembly : Path.Combine(Directory.GetCurrentDirectory(), config.Assembly);

            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var projectDir = Path.GetDirectoryName(projectFile) ?? Directory.GetCurrentDirectory();
            var tf = GetTargetFramework(projectFile);
            return Path.Combine(projectDir, "bin", config.BuildConfiguration, tf, projectName + ".dll");
        }

        private static string GetTargetFramework(string projectFile)
        {
            if (!File.Exists(projectFile)) return "net8.0";
            var content = File.ReadAllText(projectFile);
            var m = System.Text.RegularExpressions.Regex.Match(content, "<TargetFramework>(.*?)</TargetFramework>");
            return m.Success ? m.Groups[1].Value.Trim() : "net8.0";
        }

        private static Assembly LoadAssemblyWithDependencies(string assemblyPath)
        {
            var fullPath = Path.GetFullPath(assemblyPath);
            var dir = Path.GetDirectoryName(fullPath)!;
            _loadContext = new AssemblyLoadContext("MigrationContext", isCollectible: true);
            _loadContext.Resolving += (ctx, name) =>
            {
                var candidate = Path.Combine(dir, name.Name + ".dll");
                return File.Exists(candidate) ? ctx.LoadFromAssemblyPath(candidate) : null;
            };
            return _loadContext.LoadFromAssemblyPath(fullPath);
        }

        internal static IServiceProvider CreateServices(MigrationConfig config, Type[] migrationTypes)
        {
            var services = new ServiceCollection();
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
                    builder.AddSqlServer2016();
                    break;
            }
        }
    }
}
