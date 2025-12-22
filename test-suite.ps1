# ============================================================================
# FluentMigrator Wrapper - Test Suite
# Script de Testes Automatizados para CLI fm-wrapper
# ============================================================================

#!/usr/bin/env pwsh
# FluentMigrator Wrapper - Test Suite (ASCII-only)
# Simplified, stable version for PowerShell 5.1 (no Unicode)

param(
    [string]$TestCategory = "all",
    [switch]$Verbose,
    [switch]$CleanupAfter
)

$ErrorActionPreference = "Continue"

$testResults = @{ Total = 0; Passed = 0; Failed = 0; Tests = @() }

$testConfig = @{ 
    ProjectPath = (Get-Location).Path;
    TestConfigFile = "fm.config.json";
    TestConfigFileCustom = "fm.config.custom.json";
    TestLogFile = "test-results.log";
    DefaultConnectionStr = "Server=(localdb)\\mssqllocaldb;Database=FluentMigratorTestDb;Integrated Security=true;"
}

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $ts = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $entry = "[$ts] [$Level] $Message"
    Write-Host $entry
    Add-Content -Path $testConfig.TestLogFile -Value $entry
}

function Test-Command {
    param(
        [string]$TestName,
        [string]$Command,
        [string]$Description,
        [scriptblock]$Validation = $null,
        [bool]$ShouldFail = $false
    )

    $testResults.Total++
    Write-TestLog "----"
    Write-TestLog "TEST: $TestName" "TEST"
    Write-TestLog "DESC: $Description" "TEST"

    try {
        Write-TestLog "EXEC: $Command" "CMD"
        $output = Invoke-Expression $Command 2>&1
        $exitCode = $LASTEXITCODE
        if ($Verbose) { Write-Host "OUTPUT: $output" }

        if ($ShouldFail) {
            $passed = ($exitCode -ne 0)
        } else {
            $passed = ($exitCode -eq 0) -or ($output -ne $null)
        }

        if ($passed -and $Validation) {
            $passed = & $Validation $output
        }

        if ($passed) {
            Write-TestLog "RESULT: PASS" "RESULT"
            $testResults.Passed++
            $status = "PASS"
        } else {
            Write-TestLog "RESULT: FAIL" "RESULT"
            $testResults.Failed++
            $status = "FAIL"
        }

        $testResults.Tests += @{ Name = $TestName; Status = $status; Time = (Get-Date) }
        return $passed
    }
    catch {
        Write-TestLog "EXCEPTION: $_" "ERROR"
        $testResults.Failed++
        $testResults.Tests += @{ Name = $TestName; Status = "ERROR"; Time = (Get-Date) }
        return $false
    }
}

function Setup-TestConfig {
    param([string]$Language = "PT-BR", [bool]$AutoBuild = $true, [bool]$ShowSql = $true, [string]$Provider = "SqlServer")
    $c = @{ connectionString = $testConfig.DefaultConnectionStr; provider = $Provider; autoBuild = $AutoBuild; buildConfiguration = "Debug"; namespace = "FluentMigratorWrapper.Migrations"; nestedNamespaces = $false; transactionMode = "Session"; commandTimeout = 30; allowBreakingChange = $false; showSql = $ShowSql; showElapsedTime = $true; migrationsFolder = "Migrations"; defaultSchema = "dbo"; verbose = $Verbose; language = $Language }
    $json = $c | ConvertTo-Json -Depth 5
    Set-Content -Path $testConfig.TestConfigFile -Value $json
    Write-TestLog "Created config $($testConfig.TestConfigFile)" "SETUP"
}

function Setup-CustomConfig {
    param([string]$ConfigName = "fm.config.custom.json")
    if (Test-Path $testConfig.TestConfigFile) { Copy-Item $testConfig.TestConfigFile -Destination $ConfigName -Force; Write-TestLog "Copied custom config to $ConfigName" }
}

function Get-CLIPath {
    $possible = @("fm-wrapper", "./bin/Debug/net8.0/fm-wrapper.dll", "./bin/Release/net8.0/fm-wrapper.dll")
    foreach ($p in $possible) { if ((Get-Command $p -ErrorAction SilentlyContinue) -or (Test-Path $p)) { return $p } }
    return "fm-wrapper"
}

function Test-Help {
    $cli = Get-CLIPath
    Test-Command -TestName "Help - help" -Command "$cli help" -Description "Show help" -Validation { param($o) ($o -ne $null) -and ($o -match "Usage|Commands|Options|Available|USO|COMANDOS") }
    Test-Command -TestName "Help - --help" -Command "$cli --help" -Description "Show help --help" -Validation { param($o) ($o -ne $null) -and ($o -match "Usage|Commands|Options|Available|USO|COMANDOS") }
    Test-Command -TestName "Help - -h" -Command "$cli -h" -Description "Show help -h" -Validation { param($o) ($o -ne $null) -and ($o -match "Usage|Commands|Options|Available|USO|COMANDOS") }
}

function Test-Init {
    $cli = Get-CLIPath
    if (Test-Path $testConfig.TestConfigFile) { Remove-Item $testConfig.TestConfigFile -Force }
    Test-Command -TestName "Init - create" -Command "$cli init" -Description "Create default config" -Validation { param($o) Test-Path $testConfig.TestConfigFile }
    Setup-TestConfig
}

function Test-Configuration {
    $cli = Get-CLIPath
    Setup-TestConfig -Language "PT-BR"
    Setup-CustomConfig
    Test-Command -TestName "Config - custom" -Command "$cli --config=$($testConfig.TestConfigFileCustom) help" -Description "Use custom config" -Validation { param($o) ($o -ne $null) -and ($o -match "Usage|Commands|Options|Available|USO|COMANDOS") }
    Setup-TestConfig -Language "EN"
    Test-Command -TestName "Config - EN" -Command "$cli help" -Description "Help in English"
}

function Test-List {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "List - all" -Command "$cli list" -Description "List migrations"
    Test-Command -TestName "List - preview" -Command "$cli list --preview" -Description "List preview"
}

function Test-Migrate {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "Migrate - preview" -Command "$cli migrate --preview" -Description "Migrate preview"
    Setup-TestConfig -Language "PT-BR"
    Test-Command -TestName "Migrate - verbose" -Command "$cli migrate --preview" -Description "Migrate verbose"
    Setup-TestConfig -AutoBuild $false
    Test-Command -TestName "Migrate - no build" -Command "$cli migrate --preview" -Description "Migrate without building"
}

function Test-MigrateUp {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "MigrateUp - 1" -Command "$cli migrate:up --preview" -Description "Migrate up 1"
    Test-Command -TestName "MigrateUp - 2" -Command "$cli migrate:up 2 --preview" -Description "Migrate up 2"
}

function Test-MigrateDown {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "MigrateDown - 1" -Command "$cli migrate:down --preview" -Description "Migrate down 1"
    Test-Command -TestName "MigrateDown - 2" -Command "$cli migrate:down 2 --preview" -Description "Migrate down 2"
}

function Test-Rollback {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "Rollback - no args" -Command "$cli rollback" -Description "Rollback without args should fail" -ShouldFail $true
    Test-Command -TestName "Rollback - invalid" -Command "$cli rollback abc" -Description "Rollback invalid version should fail" -ShouldFail $true
}

function Test-RollbackAll {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "RollbackAll - doc" -Command "$cli help" -Description "rollback:all documented" -Validation { param($o) $o -match "rollback" }
}

function Test-Validate {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "Validate - integrity" -Command "$cli validate" -Description "Validate migrations"
}

function Test-Scaffold {
    $cli = Get-CLIPath
    Setup-TestConfig
    Test-Command -TestName "Scaffold - doc" -Command "$cli help" -Description "scaffold documented" -Validation { param($o) $o -match "scaffold" }
}

function Test-GlobalOptions {
    $cli = Get-CLIPath
    Setup-TestConfig
    Setup-CustomConfig
    Test-Command -TestName "Global --config list" -Command "$cli --config=$($testConfig.TestConfigFileCustom) list" -Description "Custom config list"
    Test-Command -TestName "Global --preview migrate" -Command "$cli migrate --preview" -Description "Preview option"
}

function Test-AdvancedFeatures {
    $cli = Get-CLIPath
    Setup-TestConfig
    $cfg = Get-Content $testConfig.TestConfigFile | ConvertFrom-Json
    $cfg.transactionMode = "Session"
    $cfg | ConvertTo-Json | Set-Content $testConfig.TestConfigFile
    Test-Command -TestName "Advanced - transaction" -Command "$cli list" -Description "Transaction mode"
}

function Test-ErrorHandling {
    $cli = Get-CLIPath
    Test-Command -TestName "Unknown command" -Command "$cli unknown-command" -Description "Unknown command should fail" -ShouldFail $true
    Test-Command -TestName "Missing config" -Command "Remove-Item $($testConfig.TestConfigFile) -Force -ErrorAction SilentlyContinue; $cli list" -Description "Missing config should fail" -ShouldFail $true
    Test-Command -TestName "Invalid JSON config" -Command "Set-Content '$($testConfig.TestConfigFile)' -Value '{invalid json'; $cli list" -Description "Invalid JSON" -ShouldFail $true
}

function Test-CombinedFlows {
    $cli = Get-CLIPath
    Test-Command -TestName "Flow Init" -Command "Remove-Item $($testConfig.TestConfigFile) -Force -ErrorAction SilentlyContinue; $cli init" -Description "Init flow"
    Setup-TestConfig
    Test-Command -TestName "Flow List" -Command "$cli list" -Description "List after init"
    Test-Command -TestName "Flow Validate" -Command "$cli validate" -Description "Validate after list"
}

function Run-TestSuite {
    if (Test-Path $testConfig.TestLogFile) { Remove-Item $testConfig.TestLogFile -Force }
    Write-TestLog "Starting test suite" "START"
    $all = @("Help","Init","Configuration","List","Migrate","MigrateUp","MigrateDown","Rollback","RollbackAll","Validate","Scaffold","GlobalOptions","AdvancedFeatures","ErrorHandling","CombinedFlows")
    switch ($TestCategory.ToLower()) {
        "all" { $run = $all }
        default { $run = $all }
    }
    foreach ($t in $run) { $fn = "Test-$t"; if (Get-Command $fn -ErrorAction SilentlyContinue) { & $fn } }

    Write-TestLog "Summary: Total=$($testResults.Total) Passed=$($testResults.Passed) Failed=$($testResults.Failed)" "SUMMARY"
    if ($CleanupAfter) { Remove-Item $testConfig.TestConfigFile -Force -ErrorAction SilentlyContinue }
    if ($testResults.Failed -eq 0) { return 0 } else { return 1 }
}

$exitCode = Run-TestSuite
exit $exitCode
