using System;
using System.Linq;
using System.Reflection;
using System.IO;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;

namespace FluentMigratorWrapper.Commands
{
    public static class CommandExecutor
    {
        public static int PrintHelp(Language language)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(MessageTranslator.Translate(language, "help_title"));
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(language, "help_usage"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(language, "help_commands"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_init"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_migrate"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_migrate_up"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_migrate_down"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_rollback"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_rollback_all"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_list"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_validate"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_scaffold"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_new"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(language, "help_options"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_config"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_preview"));
            Console.WriteLine();
            Console.WriteLine(MessageTranslator.Translate(language, "help_example"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_ex1"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_ex2"));
            Console.WriteLine(MessageTranslator.Translate(language, "help_ex3"));
            return 0;
        }

        // Exposed helper so Program can run scaffold without loading assemblies/migrations
        public static int ExecuteScaffold(string[] args, MigrationConfig config, Language language)
        {
            // Minimal option parsing for scaffold command
            var output = config.MigrationsFolder ?? "Migrations";
            var ns = config.Namespace ?? "Migrations";
            var schema = "dbo";
            var singleFileFlag = false;
            var includeDataFlag = false;
            var tablesList = new System.Collections.Generic.List<string>();

            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a.StartsWith("--output=")) output = a.Substring("--output=".Length);
                else if (a == "-o" && i + 1 < args.Length) output = args[++i];
                else if (a.StartsWith("--namespace=")) ns = a.Substring("--namespace=".Length);
                else if (a == "-n" && i + 1 < args.Length) ns = args[++i];
                else if (a.StartsWith("--schema=")) schema = a.Substring("--schema=".Length);
                else if (a == "-s" && i + 1 < args.Length) schema = args[++i];
                else if (a.StartsWith("--tables=")) tablesList.AddRange(a.Substring("--tables=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries));
                else if (a == "-t" && i + 1 < args.Length) tablesList.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
                else if (a == "--single-file") singleFileFlag = true;
                else if (a == "--include-data") includeDataFlag = true;
            }

            try
            {
                ScaffoldCommand.ExecuteScaffoldAsync(output, ns, tablesList.ToArray(), schema, singleFileFlag, includeDataFlag, config).GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(MessageTranslator.Translate(language, "error", ex.Message));
                Console.ResetColor();
                return 1;
            }
        }

        public static int ExecuteCommand(string command, string[] args, MigrationConfig config, Assembly assembly, Type[] migrationTypes, Language language)
        {
            // Handle scaffold without creating migration runner/services (scaffold doesn't need existing migrations)
            if (command == "scaffold")
            {
                // Minimal option parsing for scaffold command
                var output = config.MigrationsFolder ?? "Migrations";
                var ns = config.Namespace ?? "Migrations";
                var schema = "dbo";
                var singleFileFlag = false;
                var includeDataFlag = false;
                var tablesList = new System.Collections.Generic.List<string>();

                for (int i = 0; i < args.Length; i++)
                {
                    var a = args[i];
                    if (a.StartsWith("--output=")) output = a.Substring("--output=".Length);
                    else if (a == "-o" && i + 1 < args.Length) output = args[++i];
                    else if (a.StartsWith("--namespace=")) ns = a.Substring("--namespace=".Length);
                    else if (a == "-n" && i + 1 < args.Length) ns = args[++i];
                    else if (a.StartsWith("--schema=")) schema = a.Substring("--schema=".Length);
                    else if (a == "-s" && i + 1 < args.Length) schema = args[++i];
                    else if (a.StartsWith("--tables=")) tablesList.AddRange(a.Substring("--tables=".Length).Split(',', StringSplitOptions.RemoveEmptyEntries));
                    else if (a == "-t" && i + 1 < args.Length) tablesList.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries));
                    else if (a == "--single-file") singleFileFlag = true;
                    else if (a == "--include-data") includeDataFlag = true;
                }

                // Call scaffold
                try
                {
                    ScaffoldCommand.ExecuteScaffoldAsync(output, ns, tablesList.ToArray(), schema, singleFileFlag, includeDataFlag, config).GetAwaiter().GetResult();
                    return 0;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(MessageTranslator.Translate(language, "error", ex.Message));
                    Console.ResetColor();
                    return 1;
                }
            }

            // Handle "new migration" command to create a migration template file
            if (command == "new")
            {
                if (args.Length > 0 && args[0].ToLower() == "migration")
                {
                    // Default values
                    var output = config.MigrationsFolder ?? "Migrations";
                    var ns = config.Namespace ?? "Migrations";
                    string name = "NewMigration";

                    // Parse options from args after the 'migration' token
                    for (int i = 1; i < args.Length; i++)
                    {
                        var a = args[i];
                        if (a.StartsWith("--name=")) name = a.Substring("--name=".Length);
                        else if (a == "-n" && i + 1 < args.Length) name = args[++i];
                        else if (a.StartsWith("--output=")) output = a.Substring("--output=".Length);
                        else if (a == "-o" && i + 1 < args.Length) output = args[++i];
                        else if (a.StartsWith("--namespace=")) ns = a.Substring("--namespace=".Length);
                        else if (a == "-ns" && i + 1 < args.Length) ns = args[++i];
                        else if (!a.StartsWith("-") && name == null)
                        {
                            // positional name
                            name = a;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(name))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(MessageTranslator.Translate(language, "newmigration_name_required"));
                        Console.ResetColor();
                        return 1;
                    }

                    // Ensure output directory exists
                    Directory.CreateDirectory(output);

                    // Create timestamp version
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

                    // Sanitize class name to be a valid C# identifier (PascalCase)
                    string Sanitize(string s)
                    {
                        if (string.IsNullOrWhiteSpace(s)) return "NewMigration";
                        var parts = s.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                        var cls = string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : "")));
                        // Remove any non-alphanumeric characters
                        var cleaned = new string(cls.Where(c => char.IsLetterOrDigit(c)).ToArray());
                        if (string.IsNullOrEmpty(cleaned) || char.IsDigit(cleaned[0])) cleaned = "Migration" + cleaned;
                        return cleaned;
                    }

                    var className = Sanitize(name);
                    var fileName = $"{timestamp}_{className}.cs";
                    var filePath = Path.Combine(output, fileName);

                    // Build file content
                    var code = $@"using FluentMigrator;
using System;

namespace {ns}
{{
    [Migration({timestamp})]
    public class {className} : Migration
    {{
        public override void Up()
        {{
            // TODO: implement migration Up
        }}

        public override void Down()
        {{
            // TODO: implement migration Down
        }}
    }}
}}
";

                    try
                    {
                        File.WriteAllText(filePath, code);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(language, "newmigration_created", filePath));
                        Console.ResetColor();
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(MessageTranslator.Translate(language, "error", ex.Message));
                        Console.ResetColor();
                        return 1;
                    }
                }
            }

            // Other commands follow...

            var serviceProvider = Program.CreateServices(config, migrationTypes);

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
                            Console.WriteLine(MessageTranslator.Translate(language, "preview_mode"));
                            Console.ResetColor();
                            runner.ListMigrations();
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(MessageTranslator.Translate(language, "running_migrations"));
                            Console.ResetColor();
                            runner.MigrateUp();
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(MessageTranslator.Translate(language, "completed"));
                            Console.ResetColor();
                        }
                        break;

                    case "migrate:up":
                        var stepsUp = args.Length > 0 && int.TryParse(args[0], out var s) ? s : 1;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "migrating_up", stepsUp));
                        Console.ResetColor();
                        runner.MigrateUp(stepsUp);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(language, "completed"));
                        Console.ResetColor();
                        break;

                    case "migrate:down":
                        var stepsDown = args.Length > 0 && int.TryParse(args[0], out s) ? s : 1;
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "migrating_down", stepsDown));
                        Console.ResetColor();
                        runner.MigrateDown(stepsDown);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(language, "completed"));
                        Console.ResetColor();
                        break;

                    case "rollback":
                        if (args.Length == 0 || !long.TryParse(args[0], out var version))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(MessageTranslator.Translate(language, "rollback_invalid"));
                            Console.ResetColor();
                            return 1;
                        }
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "rollback_to", version));
                        Console.ResetColor();
                        runner.RollbackToVersion(version);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(language, "completed"));
                        Console.ResetColor();
                        break;

                    case "rollback:all":
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write(MessageTranslator.Translate(language, "rollback_confirm"));
                        Console.ResetColor();
                        var response = Console.ReadLine()?.ToLower();
                        var confirm = language == Language.EN ? response == "y" : response == "s";
                        if (confirm)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine(MessageTranslator.Translate(language, "rollback_all"));
                            Console.ResetColor();
                            runner.Rollback(int.MaxValue);
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine(MessageTranslator.Translate(language, "completed"));
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine(MessageTranslator.Translate(language, "cancelled"));
                        }
                        break;

                    case "list":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "migrations_list"));
                        Console.ResetColor();
                        Console.WriteLine();
                        runner.ListMigrations();
                        break;

                    case "help":
                    case "--help":
                    case "-h":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "help_title"));
                        Console.ResetColor();
                        Console.WriteLine();
                        Console.WriteLine(MessageTranslator.Translate(language, "help_usage"));
                        Console.WriteLine();
                        Console.WriteLine(MessageTranslator.Translate(language, "help_commands"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_init"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_migrate"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_migrate_up"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_migrate_down"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_rollback"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_rollback_all"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_list"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_validate"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_scaffold"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_new"));
                        Console.WriteLine();
                        Console.WriteLine(MessageTranslator.Translate(language, "help_options"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_config"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_preview"));
                        Console.WriteLine();
                        Console.WriteLine(MessageTranslator.Translate(language, "help_example"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_ex1"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_ex2"));
                        Console.WriteLine(MessageTranslator.Translate(language, "help_ex3"));
                        return 0;

                    case "validate":
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine(MessageTranslator.Translate(language, "validating"));
                        Console.ResetColor();
                        runner.ValidateVersionOrder();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine(MessageTranslator.Translate(language, "valid_versions"));
                        Console.ResetColor();
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(MessageTranslator.Translate(language, "unknown_command", command));
                        Console.ResetColor();
                        return 1;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(MessageTranslator.Translate(language, "error", ex.Message));
                if (ex.InnerException != null)
                {
                    Console.WriteLine(MessageTranslator.Translate(language, "details", ex.InnerException.Message));
                }
                Console.ResetColor();
                return 1;
            }
        }
    }
}
