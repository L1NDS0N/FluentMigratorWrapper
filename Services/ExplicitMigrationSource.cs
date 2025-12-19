using System;
using System.Collections.Generic;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner.Initialization;

namespace FluentMigratorWrapper.Services
{
    public sealed class ExplicitMigrationSource : IFilteringMigrationSource
    {
        private readonly IEnumerable<Type> _migrationTypes;

        public ExplicitMigrationSource(IEnumerable<Type> migrationTypes)
        {
            _migrationTypes = migrationTypes ?? Enumerable.Empty<Type>();
        }

        public IEnumerable<IMigration> GetMigrations(Func<Type, bool> predicate)
        {
            var types = _migrationTypes;

            if (predicate != null)
                types = types.Where(predicate);

            return types.Distinct().Select(t => (IMigration)Activator.CreateInstance(t)!).ToArray();
        }

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
}
