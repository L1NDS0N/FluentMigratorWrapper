using System.Text.Json.Serialization;

namespace FluentMigratorWrapper
{
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

        [JsonPropertyName("defaultSchema")]
        public string? DefaultSchema { get; set; }

        [JsonPropertyName("verbose")]
        public bool Verbose { get; set; } = false;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "PT-BR";
    }
}
