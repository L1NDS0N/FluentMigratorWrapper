Requirements: 
.Net 8 Sdk

# 1. Clonar o projeto
git clone ...
**FluentMigrator Wrapper**

Uma ferramenta simples para executar migrations do FluentMigrator a partir de um projeto .NET.
Ela compila o projeto (opcional), carrega apenas os tipos de migration encontrados e executa os comandos
do FluentMigrator (migrate, rollback, list, validate, etc.).

**Objetivo**: minimizar reflexão indesejada, melhorar logs e facilitar uso como `dotnet tool`.

**Requisitos**
- .NET 8 SDK
- `FluentMigrator.Runner` referenciado no projeto que contém as migrations (normalmente via NuGet)

**Instalação (a partir do código-fonte)**

1. Restaurar e compilar:

```powershell
dotnet restore
dotnet build -c Release
```

2. Empacotar e instalar como ferramenta (local ou global):

```powershell
dotnet pack -c Release
dotnet tool install --global --add-source ./bin/Release FluentMigratorWrapper
```

Isso cria o comando `fm-wrapper` disponível globalmente (ou use `dotnet tool install --local` para instalação por solução).

Alternativamente você pode executar com `dotnet run --project . -- [comando]` durante desenvolvimento.

**Configuração**

O arquivo de configuração padrão é `fm.config.json` no diretório onde o comando é executado. Um exemplo de `fm.config.json`:

```json
{
	"connectionString": "Server=localhost;Database=MyDatabase;User Id=sa;Password=MyPassword123;TrustServerCertificate=True;",
	"provider": "SqlServer",
	"autoBuild": true,
	"buildConfiguration": "Debug",
	"nestedNamespaces": false,
	"transactionMode": "Session",
	"commandTimeout": 30,
	"allowBreakingChange": false,
	"previewOnly": false,
	"showSql": true,
	"showElapsedTime": true,
	"migrationsFolder": "Migrations"
}
```

Campos principais:
- `connectionString` (obrigatório): string de conexão do banco.
- `provider`: um dos `SqlServer`, `PostgreSQL`, `MySql`, `SQLite`, `Oracle` (case-insensitive).
- `autoBuild`: se true, o wrapper tenta executar `dotnet build` antes de carregar o assembly.
- `buildConfiguration`: `Debug` ou `Release`.
- `namespace`: (opcional) filtra migrations por namespace.
- `nestedNamespaces`: se `true`, inclui namespaces aninhados ao filtrar.
- `transactionMode`: `Session` (padrão) ou `Transaction` (mapeia para RunnerOptions.TransactionPerSession).
- `tags`, `profile`, `allowBreakingChange`, `previewOnly`, `showSql`, `showElapsedTime` — veja `fm.config.json` para defaults.

**Uso / Comandos**

Exemplos rápidos:

```powershell
# inicializa um fm.config.json de exemplo
fm-wrapper init

# lista migrations (não altera o banco)
fm-wrapper list

# executa todas as migrations
fm-wrapper migrate

# executar em modo preview (apenas listar sql sem aplicar)
fm-wrapper migrate --preview

# sobe N migrations
fm-wrapper migrate:up 2

# desce N migrations
fm-wrapper migrate:down 1

# rollback para versão específica
fm-wrapper rollback 202501010001

# desfaz tudo (pedirá confirmação)
fm-wrapper rollback:all

# valida a ordem/versões das migrations
fm-wrapper validate
```

Opções úteis:
- `--config file.json` — usar um arquivo de configuração customizado.
- `--preview` — força preview, equivalente a `previewOnly`.

**Como funciona (resumo técnico)**

- Localiza o `.csproj` no diretório atual (ou usa `project` no config se informado).
- Opcionalmente executa `dotnet build` com a configuração escolhida.
- Localiza o assembly de saída (`bin/<Configuration>/<TargetFramework>/<ProjectName>.dll`).
- Carrega o assembly com um `AssemblyLoadContext` colecionável e resolve dependências localmente.
- Escaneia tipos públicos para classes que herdam de `FluentMigrator.Migration` e que possuam `[Migration]`.
- Registra um `IFilteringMigrationSource` explícito contendo apenas as migrations encontradas — evita reflexão desnecessária do FluentMigrator.

**Logs e Verbose**

O wrapper tem suporte a `verbose` no `fm.config.json`. Quando ativado, a ferramenta exibe logs adicionais com timestamps
e mensagens `DEBUG` que ajudam no diagnóstico. Mensagens de erro são sempre destacadas em vermelho.

**Dicas e resolução de problemas**

- Se o assembly não for encontrado, verifique `BuildConfiguration` e `TargetFramework` no `.csproj`.
- Erros de carregamento podem surgir por dependências nativas (ex.: Oracle/DB2). Verifique as pastas `runtimes/` no diretório de saída.
- Se alguma migration não aparecer, confirme que ela é pública, herda de `FluentMigrator.Migration` e tem o atributo `[Migration(...)]`.

**Contribuindo**

1. Abra uma issue descrevendo o problema ou melhoria.
2. Crie um branch e submeta um PR com mudanças pequenas e testáveis.

Se quiser, posso ajudar a transformar esse projeto em um pacote NuGet ou pipeline GitHub Actions para CI.

***

Se preferir, eu posso rodar uma build e testes básicos aqui no repositório; diga se quer que eu execute `dotnet build` agora.
