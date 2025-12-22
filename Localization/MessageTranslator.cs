using System;
using System.Collections.Generic;

namespace FluentMigratorWrapper
{
    public enum Language
    {
        PT_BR,
        EN
    }

    public static class MessageTranslator
    {
        private static readonly Dictionary<(Language, string), string> _translations = new()
        {
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

            { (Language.EN, "newmigration_name_required"), "❌ Please provide a migration name: fm-wrapper new migration MyMigration" },
            { (Language.PT_BR, "newmigration_name_required"), "❌ Forneça um nome para a migration: fm-wrapper new migration MinhaMigration" },

            { (Language.EN, "newmigration_created"), "✅ Migration created: {0}" },
            { (Language.PT_BR, "newmigration_created"), "✅ Migration criada: {0}" },

            { (Language.EN, "connectionstring_empty"), "❌ connectionString not configured!" },
            { (Language.PT_BR, "connectionstring_empty"), "❌ connectionString não configurada!" },

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

            { (Language.EN, "help_scaffold"), "  scaffold              Generate migrations from database" },
            { (Language.PT_BR, "help_scaffold"), "  scaffold              Gera migrations a partir do banco de dados" },

            { (Language.EN, "help_new"), "  new migration [NAME]   Create a new migration template" },
            { (Language.PT_BR, "help_new"), "  new migration [NOME]   Cria um template de migration" },

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
            return key;
        }
    }
}
