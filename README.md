# FluentMigrator Wrapper

> **Language**: [🇬🇧 English](#english) | [🇧🇷 Português Brasileiro](#português-brasileiro)

---

## English

### Overview

**FluentMigrator Wrapper** is a CLI tool to streamline database migrations using FluentMigrator in .NET projects. It automates project compilation, discovers migration types, and provides a clean interface for executing migration commands with support for multiple databases, scaffolding, and advanced features.

**Key Features**:
- Automatic project build (optional)
- Multiple database providers (SQL Server, PostgreSQL, MySQL, SQLite, Oracle)
- Migration scaffolding from existing databases
- Tag-based migration execution
- Profile support for environment-specific configurations
- Transaction modes (Session or Transaction)
- Verbose logging and SQL preview
- Bilingual support (English & Portuguese)

### Requirements

- .NET 8 SDK or later
- `FluentMigrator.Runner` NuGet package in your project

### Installation

**From Source:**

```powershell
dotnet restore
dotnet build -c Release
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper
```

This installs `fm-wrapper` globally. For local (per-solution) installation, use `--local` flag instead of `--global`.

**During Development:**

```powershell
dotnet run --project . -- [command] [options]
```

### Configuration

Create `fm.config.json` in your project root (or specify a custom path with `--config`):

```json
{
  "connectionString": "Server=localhost;Database=MyDatabase;User Id=sa;Password=MyPassword123;TrustServerCertificate=True;",
  "provider": "SqlServer",
  "autoBuild": true,
  "buildConfiguration": "Debug",
  "namespace": "YourProject.Migrations",
  "nestedNamespaces": false,
  "transactionMode": "Session",
  "commandTimeout": 30,
  "allowBreakingChange": false,
  "showSql": true,
  "showElapsedTime": true,
  "migrationsFolder": "Migrations",
  "defaultSchema": "dbo",
  "verbose": false,
  "language": "EN"
}
```

**Configuration Fields:**

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `connectionString` | string | required | Database connection string |
| `provider` | string | `SqlServer` | Database provider: `SqlServer`, `PostgreSQL`, `MySql`, `SQLite`, `Oracle` |
| `autoBuild` | boolean | `true` | Run `dotnet build` before migrations |
| `buildConfiguration` | string | `Debug` | Build configuration: `Debug` or `Release` |
| `namespace` | string | optional | Filter migrations by namespace |
| `nestedNamespaces` | boolean | `false` | Include nested namespaces when filtering |
| `transactionMode` | string | `Session` | Transaction handling: `Session` or `Transaction` |
| `commandTimeout` | int | `30` | Command timeout in seconds |
| `allowBreakingChange` | boolean | `false` | Allow breaking changes in migrations |
| `showSql` | boolean | `true` | Display generated SQL statements |
| `showElapsedTime` | boolean | `true` | Show execution time |
| `migrationsFolder` | string | `Migrations` | Migrations directory path |
| `defaultSchema` | string | optional | Default database schema |
| `tags` | array | optional | Tag filters for migrations |
| `profile` | string | optional | Execution profile name |
| `verbose` | boolean | `false` | Enable verbose logging |
| `language` | string | `PT-BR` | UI language: `EN` or `PT-BR` |

### Commands

#### Initialize Configuration

Create a default `fm.config.json` file:

```powershell
fm-wrapper init
```

#### List Migrations

Display all available migrations:

```powershell
fm-wrapper list
```

#### Execute All Pending Migrations

```powershell
fm-wrapper migrate
fm-wrapper migrate --preview    # Preview without executing
```

#### Migrate Up/Down

Move forward or backward by a specific number of steps:

```powershell
fm-wrapper migrate:up 2         # Execute next 2 migrations
fm-wrapper migrate:down 1       # Rollback last 1 migration
```

#### Rollback

Rollback to a specific migration or all migrations:

```powershell
fm-wrapper rollback 202501010001  # Rollback to specific migration
fm-wrapper rollback:all           # Rollback all migrations
```

#### Validate

Check migration integrity:

```powershell
fm-wrapper validate
```

#### Scaffold Database

Generate migrations from an existing database:

```powershell
fm-wrapper scaffold
fm-wrapper scaffold --output ./Migrations --namespace "MyApp.Migrations"
fm-wrapper scaffold --tables Users Products --schema dbo
fm-wrapper scaffold --single-file --include-data
```

#### Create New Migration (template)

Generate a new migration file template with timestamp and skeleton `Up`/`Down` methods.

```powershell
fm-wrapper new migration CreateUsersTable
fm-wrapper new migration --name CreateUsersTable --output ./Migrations --namespace "MyApp.Migrations"
```

Options:
- `--name, -n` — Migration class name (positional name is also accepted)
- `--output, -o` — Output directory (default: `Migrations`)
- `--namespace` — Namespace for the generated migration (default: `Migrations`)


**Scaffold Options:**

- `--output, -o` — Output directory (default: `Migrations`)
- `--namespace, -n` — Generated migration namespace (default: `Migrations`)
- `--tables, -t` — Specific tables (if omitted, all tables included)
- `--schema, -s` — Database schema (default: `dbo`)
- `--single-file` — Generate single migration file
- `--include-data` — Include existing data in migrations
 - `--separate-data-files` — When used with `--include-data`, generates one seed file per table instead of a single combined seed file (creates a subfolder under the output folder).
 - `--seed-folder` — Name of the subfolder inside the `--output` folder where per-table seed files will be placed (default: `seed`).

#### Display Help

```powershell
fm-wrapper help
fm-wrapper --help
fm-wrapper -h
```

### Global Options

- `--config file.json` — Use custom configuration file
- `--preview` — Force preview mode (no changes applied)

---

## Português Brasileiro

### Visão Geral

**FluentMigrator Wrapper** é uma ferramenta CLI para simplificar migrações de banco de dados usando FluentMigrator em projetos .NET. Automatiza a compilação do projeto, descobre tipos de migrations e fornece uma interface limpa para executar comandos de migração com suporte a múltiplos bancos de dados, scaffolding e recursos avançados.

**Funcionalidades Principais**:
- Build automático do projeto (opcional)
- Múltiplos provedores de banco de dados (SQL Server, PostgreSQL, MySQL, SQLite, Oracle)
- Geração de migrations a partir de bancos de dados existentes (scaffolding)
- Execução de migrations por tags
- Suporte a perfis para configurações específicas de ambiente
- Modos de transação (Session ou Transaction)
- Logging verboso e visualização de SQL
- Suporte bilíngue (Inglês & Português)

### Requisitos

- .NET 8 SDK ou superior
- Pacote NuGet `FluentMigrator.Runner` no seu projeto

### Instalação

**A partir do código-fonte:**

```powershell
dotnet restore
dotnet build -c Release
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper
```

Isso instala `fm-wrapper` globalmente. Para instalação local (por solução), use a flag `--local` em vez de `--global`.

**Durante o desenvolvimento:**

```powershell
dotnet run --project . -- [comando] [opções]
```

### Configuração

Crie `fm.config.json` no diretório raiz do seu projeto (ou especifique um caminho customizado com `--config`):

```json
{
  "connectionString": "Server=localhost;Database=MeuBancoDados;User Id=sa;Password=MinhaSenh@123;TrustServerCertificate=True;",
  "provider": "SqlServer",
  "autoBuild": true,
  "buildConfiguration": "Debug",
  "namespace": "SeuProjeto.Migrations",
  "nestedNamespaces": false,
  "transactionMode": "Session",
  "commandTimeout": 30,
  "allowBreakingChange": false,
  "showSql": true,
  "showElapsedTime": true,
  "migrationsFolder": "Migrations",
  "defaultSchema": "dbo",
  "verbose": false,
  "language": "PT-BR"
}
```

**Campos de Configuração:**

| Campo | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `connectionString` | string | obrigatório | String de conexão do banco de dados |
| `provider` | string | `SqlServer` | Provedor de banco: `SqlServer`, `PostgreSQL`, `MySql`, `SQLite`, `Oracle` |
| `autoBuild` | boolean | `true` | Executa `dotnet build` antes das migrations |
| `buildConfiguration` | string | `Debug` | Configuração de build: `Debug` ou `Release` |
| `namespace` | string | opcional | Filtra migrations por namespace |
| `nestedNamespaces` | boolean | `false` | Inclui namespaces aninhados ao filtrar |
| `transactionMode` | string | `Session` | Gerenciamento de transações: `Session` ou `Transaction` |
| `commandTimeout` | int | `30` | Timeout do comando em segundos |
| `allowBreakingChange` | boolean | `false` | Permite mudanças disruptivas em migrations |
| `showSql` | boolean | `true` | Exibe instruções SQL geradas |
| `showElapsedTime` | boolean | `true` | Mostra tempo de execução |
| `migrationsFolder` | string | `Migrations` | Caminho do diretório de migrations |
| `defaultSchema` | string | opcional | Schema padrão do banco de dados |
| `tags` | array | opcional | Filtros de tags para migrations |
| `profile` | string | opcional | Nome do perfil de execução |
| `verbose` | boolean | `false` | Ativa logging verboso |
| `language` | string | `PT-BR` | Idioma da interface: `EN` ou `PT-BR` |

### Comandos

#### Inicializar Configuração

Cria um arquivo padrão `fm.config.json`:

```powershell
fm-wrapper init
```

#### Listar Migrations

Exibe todas as migrations disponíveis:

```powershell
fm-wrapper list
```

#### Executar Todas as Migrations Pendentes

```powershell
fm-wrapper migrate
fm-wrapper migrate --preview    # Visualiza sem executar
```

#### Migrar para Cima/Baixo

Avança ou retrocede por um número específico de passos:

```powershell
fm-wrapper migrate:up 2         # Executa as próximas 2 migrations
fm-wrapper migrate:down 1       # Desfaz a última 1 migration
```

#### Reverter

Reverte para uma migration específica ou todas as migrations:

```powershell
fm-wrapper rollback 202501010001  # Reverte para migration específica
fm-wrapper rollback:all           # Reverte todas as migrations
```

#### Validar

Valida a integridade das migrations:

```powershell
fm-wrapper validate
```

#### Gerar Migrations do Banco

Gera migrations a partir de um banco de dados existente:

```powershell
fm-wrapper scaffold
fm-wrapper scaffold --output ./Migrations --namespace "MeuApp.Migrations"
fm-wrapper scaffold --tables Usuarios Produtos --schema dbo
fm-wrapper scaffold --single-file --include-data
fm-wrapper scaffold --include-data --separate-data-files
fm-wrapper scaffold --include-data --separate-data-files --seed-folder seeds
```

**Opções de Scaffold:**

- `--output, -o` — Diretório de saída (padrão: `Migrations`)
- `--namespace, -n` — Namespace para migrations geradas (padrão: `Migrations`)
- `--tables, -t` — Tabelas específicas (se omitido, todas são incluídas)
- `--schema, -s` — Schema do banco de dados (padrão: `dbo`)
- `--single-file` — Gera uma única migration
- `--include-data` — Inclui dados existentes nas migrations
 - `--separate-data-files` — Quando usado com `--include-data`, gera um arquivo de seed por tabela em vez de um único arquivo (cria uma subpasta dentro do diretório de saída).
 - `--seed-folder` — Nome da subpasta dentro de `--output` onde os arquivos de seed por tabela serão colocados (padrão: `seed`).

#### Exibir Ajuda

```powershell
fm-wrapper help
fm-wrapper --help
fm-wrapper -h
```

### Opções Globais

- `--config arquivo.json` — Usar arquivo de configuração customizado
- `--preview` — Força modo de visualização (sem aplicar mudanças)

---

**Última Atualização**: 19 de Dezembro de 2025
