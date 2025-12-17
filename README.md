# FluentMigrator Wrapper

> **Language**: [🇬🇧 English](#english) | [🇧🇷 Português Brasileiro](#português-brasileiro)

---

## English

### Overview

**FluentMigrator Wrapper** is a simple tool to execute FluentMigrator migrations from a .NET project. It compiles the project (optional), loads only discovered migration types, and executes FluentMigrator commands (migrate, rollback, list, validate, etc.).

**Goal**: minimize unnecessary reflection, improve logging, and facilitate use as a \dotnet tool\.

### Requirements

- .NET 8 SDK
- \FluentMigrator.Runner\ referenced in the project containing migrations (typically via NuGet)

### Installation (from source)

1. Restore and build:

\\\powershell
dotnet restore
dotnet build -c Release
\\\

2. Pack and install as a tool (globally or locally):

\\\powershell
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper
\\\

This creates the \m-wrapper\ command available globally (or use \dotnet tool install --local\ for per-solution installation).

Alternatively, run with \dotnet run --project . -- [command]\ during development.

### Configuration

The default configuration file is \m.config.json\ in the directory where the command is executed. Example \m.config.json\:

\\\json
{
  ""connectionString"": ""Server=localhost;Database=MyDatabase;User Id=sa;Password=MyPassword123;TrustServerCertificate=True;"",
  ""provider"": ""SqlServer"",
  ""autoBuild"": true,
  ""buildConfiguration"": ""Debug"",
  ""nestedNamespaces"": false,
  ""transactionMode"": ""Session"",
  ""commandTimeout"": 30,
  ""allowBreakingChange"": false,
  ""previewOnly"": false,
  ""showSql"": true,
  ""showElapsedTime"": true,
  ""migrationsFolder"": ""Migrations"",
  ""language"": ""EN""
}
\\\

**Main fields**:
- \connectionString\ (required): database connection string.
- \provider\: one of \SqlServer\, \PostgreSQL\, \MySql\, \SQLite\, \Oracle\ (case-insensitive).
- \utoBuild\: if \	rue\, executes \dotnet build\ before loading the assembly.
- \uildConfiguration\: \Debug\ or \Release\.
- \
amespace\: (optional) filters migrations by namespace.
- \
estedNamespaces\: if \	rue\, includes nested namespaces when filtering.
- \	ransactionMode\: \Session\ (default) or \Transaction\ (maps to RunnerOptions.TransactionPerSession).
- \language\: \EN\ (English) or \PT-BR\ (Portuguese, default).

### Usage / Commands

Quick examples:

\\\powershell
fm-wrapper init
fm-wrapper list
fm-wrapper migrate
fm-wrapper migrate --preview
fm-wrapper migrate:up 2
fm-wrapper migrate:down 1
fm-wrapper rollback 202501010001
fm-wrapper rollback:all
fm-wrapper validate
\\\

**Useful options**:
- \--config file.json\ — use a custom configuration file.
- \--preview\ — force preview mode.

---

## Português Brasileiro

### Visão Geral

**FluentMigrator Wrapper** é uma ferramenta simples para executar migrations do FluentMigrator a partir de um projeto .NET. Ela compila o projeto (opcional), carrega apenas os tipos de migration encontrados e executa os comandos do FluentMigrator.

**Objetivo**: minimizar reflexão indesejada, melhorar logs e facilitar uso como \dotnet tool\.

### Requisitos

- .NET 8 SDK
- \FluentMigrator.Runner\ referenciado no projeto que contém as migrations (normalmente via NuGet)

### Instalação (a partir do código-fonte)

1. Restaurar e compilar:

\\\powershell
dotnet restore
dotnet build -c Release
\\\

2. Empacotar e instalar como ferramenta (global ou local):

\\\powershell
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper
\\\

Isso cria o comando \m-wrapper\ disponível globalmente.

Alternativamente você pode executar com \dotnet run --project . -- [comando]\ durante desenvolvimento.

### Configuração

O arquivo de configuração padrão é \m.config.json\ no diretório onde o comando é executado. Um exemplo de \m.config.json\:

\\\json
{
  ""connectionString"": ""Server=localhost;Database=MyDatabase;User Id=sa;Password=MyPassword123;TrustServerCertificate=True;"",
  ""provider"": ""SqlServer"",
  ""autoBuild"": true,
  ""buildConfiguration"": ""Debug"",
  ""nestedNamespaces"": false,
  ""transactionMode"": ""Session"",
  ""commandTimeout"": 30,
  ""allowBreakingChange"": false,
  ""previewOnly"": false,
  ""showSql"": true,
  ""showElapsedTime"": true,
  ""migrationsFolder"": ""Migrations"",
  ""language"": ""PT-BR""
}
\\\

**Campos principais**:
- \connectionString\ (obrigatório): string de conexão do banco.
- \provider\: um dos \SqlServer\, \PostgreSQL\, \MySql\, \SQLite\, \Oracle\ (case-insensitive).
- \utoBuild\: se \	rue\, tenta executar \dotnet build\ antes de carregar o assembly.
- \uildConfiguration\: \Debug\ ou \Release\.
- \
amespace\: (opcional) filtra migrations por namespace.
- \
estedNamespaces\: se \	rue\, inclui namespaces aninhados ao filtrar.
- \	ransactionMode\: \Session\ (padrão) ou \Transaction\.
- \language\: \PT-BR\ (Português, padrão) ou \EN\ (English).

### Uso / Comandos

Exemplos rápidos:

\\\powershell
fm-wrapper init
fm-wrapper list
fm-wrapper migrate
fm-wrapper migrate --preview
fm-wrapper migrate:up 2
fm-wrapper migrate:down 1
fm-wrapper rollback 202501010001
fm-wrapper rollback:all
fm-wrapper validate
\\\

**Opções úteis**:
- \--config file.json\ — usar um arquivo de configuração customizado.
- \--preview\ — força preview mode.

---

**Última Atualização**: 17 de Dezembro de 2025
