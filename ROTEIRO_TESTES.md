# 📋 Roteiro de Testes - FluentMigrator Wrapper CLI

Este documento contém um roteiro completo para testar todas as features do CLI `fm-wrapper`.

## ✅ Pré-requisitos

- .NET 8 SDK instalado
- Banco de dados SQL Server (ou outro provider) disponível
- Um projeto .NET com migrations em FluentMigrator
- Build da ferramenta compilado

---

## 🎯 Testes por Feature

### 1. **Help e Informações**

#### 1.1 - Exibir Help (variações)
```powershell
# Teste com help
fm-wrapper help

# Teste com --help
fm-wrapper --help

# Teste com -h
fm-wrapper -h
```
**Validação**: Deve exibir lista de comandos, opções e exemplos

#### 1.2 - Help com arquivo de config diferente
```powershell
fm-wrapper --config=custom.json help
```
**Validação**: Deve usar idioma do arquivo de config (se existir)

---

### 2. **Inicialização (Init)**

#### 2.1 - Criar configuração padrão
```powershell
# Remover config existente (se houver)
Remove-Item fm.config.json -Force -ErrorAction SilentlyContinue

# Criar nova config
fm-wrapper init
```
**Validação**: Deve criar `fm.config.json` com valores padrão

#### 2.2 - Init com arquivo já existente
```powershell
# Executar init novamente
fm-wrapper init

# Responder 'n' (ou 'não' em PT-BR) quando perguntado
```
**Validação**: Deve perguntar se quer sobrescrever e não criar se responder "não"

#### 2.3 - Init com arquivo já existente e confirmar sobrescrita
```powershell
# Executar init novamente
fm-wrapper init

# Responder 'y' (ou 'sim' em PT-BR) quando perguntado
```
**Validação**: Deve sobrescrever arquivo anterior

---

### 3. **Configuração (Config)**

#### 3.1 - Usar configuração padrão
```powershell
# Editar fm.config.json com suas credenciais reais
# Campos essenciais:
# - connectionString: Sua conexão SQL Server
# - provider: SqlServer (ou outro)

fm-wrapper list
```
**Validação**: Deve ler `fm.config.json` e funcionar

#### 3.2 - Usar arquivo de config customizado
```powershell
# Criar arquivo custom-config.json com conteúdo
Copy-Item fm.config.json custom-config.json

fm-wrapper --config=custom-config.json list
```
**Validação**: Deve usar arquivo customizado especificado

#### 3.3 - Config com diferentes languages
```powershell
# Editar fm.config.json: "language": "EN"
# Executar comando
fm-wrapper help

# Editar fm.config.json: "language": "PT-BR"
# Executar comando
fm-wrapper help
```
**Validação**: Mensagens devem aparecer em inglês ou português conforme config

---

### 4. **Listar Migrations (List)**

#### 4.1 - Listar todas as migrations
```powershell
fm-wrapper list
```
**Validação**: Deve exibir todas as migrations disponíveis

#### 4.2 - List com preview
```powershell
fm-wrapper list --preview
```
**Validação**: Deve exibir list em modo preview

#### 4.3 - List com config customizado
```powershell
fm-wrapper --config=custom-config.json list
```
**Validação**: Deve funcionar com arquivo config customizado

---

### 5. **Executar Migrations (Migrate)**

#### 5.1 - Executar todas as migrations pendentes
```powershell
fm-wrapper migrate
```
**Validação**: Deve executar todas as migrations não aplicadas

#### 5.2 - Migrate em modo preview
```powershell
fm-wrapper migrate --preview
```
**Validação**: Deve exibir o que faria SEM executar

#### 5.3 - Migrate com verbose
```powershell
# Editar fm.config.json: "verbose": true
fm-wrapper migrate
```
**Validação**: Deve exibir mais detalhes da execução

#### 5.4 - Migrate sem auto-build
```powershell
# Editar fm.config.json: "autoBuild": false
fm-wrapper migrate
```
**Validação**: Não deve compilar projeto, usar assembly existente

#### 5.5 - Migrate com build automático
```powershell
# Editar fm.config.json: "autoBuild": true
fm-wrapper migrate
```
**Validação**: Deve compilar projeto antes de migrar

#### 5.6 - Migrate com diferentes providers
```powershell
# PostgreSQL
# Editar fm.config.json: 
# "provider": "PostgreSQL"
# "connectionString": <postgres connection>
fm-wrapper migrate

# MySQL
# "provider": "MySql"
fm-wrapper migrate

# SQLite
# "provider": "SQLite"
fm-wrapper migrate

# Oracle
# "provider": "Oracle"
fm-wrapper migrate
```
**Validação**: Deve funcionar com cada provider

---

### 6. **Migrate Up (Passos para Frente)**

#### 6.1 - Migrate up padrão (1 passo)
```powershell
fm-wrapper migrate:up
```
**Validação**: Deve aplicar 1 migration

#### 6.2 - Migrate up com número específico
```powershell
fm-wrapper migrate:up 2
fm-wrapper migrate:up 5
```
**Validação**: Deve aplicar exatamente o número de steps informado

#### 6.3 - Migrate up com 0 steps
```powershell
fm-wrapper migrate:up 0
```
**Validação**: Não deve fazer nada ou avisar que são 0 steps

---

### 7. **Migrate Down (Passos para Trás)**

#### 7.1 - Migrate down padrão (1 passo)
```powershell
fm-wrapper migrate:down
```
**Validação**: Deve reverter 1 migration

#### 7.2 - Migrate down com número específico
```powershell
fm-wrapper migrate:down 2
fm-wrapper migrate:down 5
```
**Validação**: Deve reverter exatamente o número de steps informado

#### 7.3 - Migrate down com mais steps que migrations aplicadas
```powershell
fm-wrapper migrate:down 100
```
**Validação**: Deve voltar até a primeira migration ou avisar

---

### 8. **Rollback para Versão (Rollback)**

#### 8.1 - Rollback para versão específica
```powershell
# Assumindo migração com versão 202501010001
fm-wrapper rollback 202501010001
```
**Validação**: Deve fazer rollback até a versão especificada

#### 8.2 - Rollback sem argumentos (erro)
```powershell
fm-wrapper rollback
```
**Validação**: Deve exibir erro informando que versão é obrigatória

#### 8.3 - Rollback com versão inválida (não numérica)
```powershell
fm-wrapper rollback abc123
```
**Validação**: Deve exibir erro de formato inválido

---

### 9. **Rollback Completo (Rollback All)**

#### 9.1 - Rollback de todas as migrations (com confirmação)
```powershell
# Quando perguntado, responder 'y' (ou 's' em PT-BR)
fm-wrapper rollback:all
```
**Validação**: Deve reverter TODAS as migrations após confirmação

#### 9.2 - Rollback all (recusar)
```powershell
# Quando perguntado, responder 'n' (ou 'n' em PT-BR)
fm-wrapper rollback:all
```
**Validação**: Deve cancelar operação se responder não

#### 9.3 - Rollback all com preview
```powershell
fm-wrapper rollback:all --preview
```
**Validação**: Comportamento esperado em modo preview

---

### 10. **Validação de Migrations (Validate)**

#### 10.1 - Validar integridade das migrations
```powershell
fm-wrapper validate
```
**Validação**: Deve verificar se versions estão em ordem e avisar se houver problema

#### 10.2 - Validate com migrations em ordem correta
```powershell
# Migrations: 202501010001, 202501010002, 202501010003
fm-wrapper validate
```
**Validação**: Deve exibir mensagem de sucesso

#### 10.3 - Validate com migrations fora de ordem
```powershell
# Criar migration com version anterior à atual
# Migrations: 202501010001, 202501010005, 202501010002
fm-wrapper validate
```
**Validação**: Deve avisar sobre problema na ordem

---

### 11. **Scaffolding (Scaffold)**

#### 11.1 - Scaffold básico (todas as tabelas)
```powershell
fm-wrapper scaffold
```
**Validação**: Deve gerar migration file com todas as tabelas do banco

#### 11.2 - Scaffold com output customizado
```powershell
fm-wrapper scaffold --output=./CustomMigrations
fm-wrapper scaffold -o CustomMigrations
```
**Validação**: Deve criar migrations no diretório especificado

#### 11.3 - Scaffold com namespace customizado
```powershell
fm-wrapper scaffold --namespace="MyApp.Custom.Migrations"
fm-wrapper scaffold -n "MyApp.Custom.Migrations"
```
**Validação**: Classe gerada deve usar namespace especificado

#### 11.4 - Scaffold de tabelas específicas
```powershell
fm-wrapper scaffold --tables=Users,Products
fm-wrapper scaffold -t Users,Products
```
**Validação**: Deve gerar migrations apenas para as tabelas listadas

#### 11.5 - Scaffold com schema específico
```powershell
fm-wrapper scaffold --schema=dbo
fm-wrapper scaffold -s dbo
```
**Validação**: Deve considerar apenas tabelas do schema informado

#### 11.6 - Scaffold em arquivo único
```powershell
fm-wrapper scaffold --single-file
```
**Validação**: Deve gerar uma única migration com todas as tabelas

#### 11.7 - Scaffold incluindo dados
```powershell
fm-wrapper scaffold --include-data
```
**Validação**: Deve gerar seeds/inserts com dados do banco

#### 11.8 - Scaffold com múltiplas opções
```powershell
fm-wrapper scaffold --output=./Migrations --namespace="App.DB.Migrations" --tables=Users,Orders --schema=dbo --single-file --include-data
```
**Validação**: Deve respeitar todas as opções combinadas

#### 11.9 - Scaffold sem arquivo de config (erro)
```powershell
# Remover fm.config.json
rm fm.config.json

# Tentar scaffold
fm-wrapper scaffold
```
**Validação**: Deve exibir erro pedindo arquivo config

---

### 12. **Opções Globais**

#### 12.1 - Config em todas as operações
```powershell
fm-wrapper --config=custom.json migrate
fm-wrapper --config=custom.json list
fm-wrapper --config=custom.json scaffold
```
**Validação**: Todas operações devem funcionar com --config

#### 12.2 - Preview em todas as operações de migração
```powershell
fm-wrapper migrate --preview
fm-wrapper migrate:up 2 --preview
fm-wrapper migrate:down 1 --preview
```
**Validação**: Deve exibir em modo preview sem aplicar

#### 12.3 - Config preview em arquivo
```powershell
# Editar fm.config.json: "previewOnly": true
fm-wrapper migrate
```
**Validação**: Deve funcionar como --preview

---

### 13. **Recursos Avançados**

#### 13.1 - Tags (Tag-based Migrations)
```powershell
# Editar fm.config.json: "tags": ["production", "critical"]
fm-wrapper migrate
```
**Validação**: Deve executar apenas migrations com tags especificadas

#### 13.2 - Profile (Configuração por Ambiente)
```powershell
# Editar fm.config.json: "profile": "staging"
fm-wrapper migrate
```
**Validação**: Deve usar profile se migrations o suportarem

#### 13.3 - Transaction Mode (Session)
```powershell
# Editar fm.config.json: "transactionMode": "Session"
fm-wrapper migrate
```
**Validação**: Cada migração em sua própria transação

#### 13.4 - Transaction Mode (Transaction)
```powershell
# Editar fm.config.json: "transactionMode": "Transaction"
fm-wrapper migrate
```
**Validação**: Todas as migrações em uma transação

#### 13.5 - Command Timeout
```powershell
# Editar fm.config.json: "commandTimeout": 60
fm-wrapper migrate
```
**Validação**: Deve usar timeout especificado (não causa erro em testes normais)

#### 13.6 - Allow Breaking Changes
```powershell
# Editar fm.config.json: "allowBreakingChange": true
fm-wrapper migrate
```
**Validação**: Deve permitir breaking changes

#### 13.7 - Show SQL
```powershell
# Editar fm.config.json: "showSql": true
fm-wrapper migrate
```
**Validação**: Deve exibir SQL gerado

#### 13.8 - Show Elapsed Time
```powershell
# Editar fm.config.json: "showElapsedTime": true
fm-wrapper migrate
```
**Validação**: Deve exibir tempo de execução

#### 13.9 - Nested Namespaces
```powershell
# Editar fm.config.json: "nestedNamespaces": true
# Com estrutura de pasta: Migrations/v1/Migration001.cs
fm-wrapper list
```
**Validação**: Deve encontrar migrations em namespaces aninhados

---

### 14. **Validações de Erro**

#### 14.1 - Comando desconhecido
```powershell
fm-wrapper unknown-command
```
**Validação**: Deve exibir erro e sugerir comando válido

#### 14.2 - Config inválida (JSON malformado)
```powershell
# Editar fm.config.json com JSON inválido
fm-wrapper migrate
```
**Validação**: Deve exibir erro de parsing

#### 14.3 - Conexão ao banco falha
```powershell
# Editar fm.config.json com connection string inválida
fm-wrapper migrate
```
**Validação**: Deve exibir erro de conexão

#### 14.4 - Assembly não encontrado
```powershell
# Editar fm.config.json: "assembly": "./inexistente.dll"
fm-wrapper migrate
```
**Validação**: Deve exibir erro de arquivo não encontrado

#### 14.5 - Projeto não encontrado
```powershell
# Remover .csproj ou editar "project" com path inválido
fm-wrapper migrate
```
**Validação**: Deve exibir erro de projeto não encontrado

#### 14.6 - Build falha
```powershell
# Adicionar erro de compilação no projeto
# Editar fm.config.json: "autoBuild": true
fm-wrapper migrate
```
**Validação**: Deve exibir erro de build e não continuar

#### 14.7 - Sem migrations encontradas
```powershell
# Criar assembly sem migrations
fm-wrapper list
```
**Validação**: Deve avisar que nenhuma migration foi encontrada

---

### 15. **Combinações e Fluxos Reais**

#### 15.1 - Fluxo inicial de setup
```powershell
# Limpar
Remove-Item fm.config.json -Force -ErrorAction SilentlyContinue

# Setup inicial
fm-wrapper init
# Editar fm.config.json com valores reais
fm-wrapper list
fm-wrapper migrate --preview
fm-wrapper migrate
```
**Validação**: Tudo deve funcionar em sequência

#### 15.2 - Fluxo de desenvolvimento
```powershell
# Criar nova migration no projeto
# Editar a migration
fm-wrapper migrate --preview
fm-wrapper migrate
fm-wrapper list
```
**Validação**: Nova migration deve aparecer e ser aplicada

#### 15.3 - Fluxo de rollback e retry
```powershell
# Aplicar migrações
fm-wrapper migrate

# Verificar lista
fm-wrapper list

# Voltar uma
fm-wrapper migrate:down 1

# Aplicar novamente
fm-wrapper migrate:up 1
```
**Validação**: Estado do banco deve ser consistente

#### 15.4 - Fluxo de múltiplos ambientes
```powershell
# Dev
fm-wrapper --config=fm.config.dev.json migrate

# Staging
fm-wrapper --config=fm.config.staging.json migrate

# Prod
fm-wrapper --config=fm.config.prod.json migrate --preview
```
**Validação**: Cada ambiente deve usar sua config

#### 15.5 - Fluxo de scaffolding e aplicação
```powershell
# Scaffold do banco existente
fm-wrapper scaffold --output=./Migrations --namespace="App.Migrations"

# Verificar gerado
fm-wrapper list

# Aplicar se tiver pendente
fm-wrapper migrate
```
**Validação**: Migrations geradas devem ser válidas e aplicáveis

---

## 📊 Checklist Final

- [ ] Todos os comandos executam sem erros
- [ ] Help exibe informações corretas
- [ ] Init cria config válida
- [ ] List exibe todas as migrations
- [ ] Migrate/Up/Down/Rollback/Rollback:all funcionam
- [ ] Validate identifica problemas
- [ ] Scaffold gera migrations válidas
- [ ] Config customizada funciona
- [ ] Languages (EN/PT-BR) funcionam
- [ ] Preview mode funciona
- [ ] Todos os providers suportados funcionam
- [ ] Todos os campos de config funcionam
- [ ] Erros exibem mensagens apropriadas
- [ ] Fluxos combinados funcionam

---

## 🐛 Casos de Edge Cases

1. **Zero migrations** - Testar com projeto vazio de migrations
2. **Milhares de migrations** - Testar performance com muitas migrations
3. **Nomes especiais** - Migrations com caracteres especiais
4. **Namespaces profundos** - Estrutura muito aninhada
5. **Banco inacessível** - Comportamento sem conectividade
6. **Permissões insuficientes** - Usuário sem privilégios de DDL
7. **Migration muito longa** - Timeout de comando
8. **Caracteres unicode** - Nomes/comments com unicode
9. **Múltiplas execuções simultâneas** - Concorrência

---

## 📝 Notas

- Sempre ter backup antes de testes com dados reais
- Usar um banco de testes para validações
- Manter arquivo de config PT-BR e EN para testes
- Documentar issues encontradas durante testes
