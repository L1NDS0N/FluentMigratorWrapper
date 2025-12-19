using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentMigrator;

namespace FluentMigratorWrapper
{
    public class MigrationAssemblyScanner
    {
        public static Type[] GetMigrationTypes(Assembly assembly, string? namespaceFilter, bool nestedNamespaces, bool verbose)
        {
            var migrationTypes = new List<Type>();

            try
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine("   Tentando GetExportedTypes()...");
                    Console.ResetColor();
                }

                var exportedTypes = assembly.GetExportedTypes();

                foreach (var type in exportedTypes)
                {
                    try
                    {
                        if (IsMigrationType(type, namespaceFilter, nestedNamespaces))
                        {
                            migrationTypes.Add(type);
                            if (verbose)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.WriteLine($"   ✓ {type.FullName}");
                                Console.ResetColor();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (verbose)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            Console.WriteLine($"   ⚠ Ignorando tipo: {ex.Message}");
                            Console.ResetColor();
                        }
                        continue;
                    }
                }

                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"   {migrationTypes.Count} migration(s) encontrada(s) via GetExportedTypes");
                    Console.ResetColor();
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("   ⚠ GetExportedTypes falhou, tentando tipos carregados...");
                    Console.ResetColor();
                }

                var loadedTypes = ex.Types.Where(t => t != null).ToArray();

                foreach (var type in loadedTypes)
                {
                    try
                    {
                        if (type != null && IsMigrationType(type, namespaceFilter, nestedNamespaces))
                        {
                            migrationTypes.Add(type);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"   ⚠ Erro ao escanear: {ex.Message}");
                    Console.ResetColor();
                }

                try
                {
                    var allTypes = assembly.GetTypes();
                    foreach (var type in allTypes)
                    {
                        try
                        {
                            if (IsMigrationType(type, namespaceFilter, nestedNamespaces))
                            {
                                migrationTypes.Add(type);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex2)
                {
                    var loadedTypes = ex2.Types.Where(t => t != null).ToArray();
                    foreach (var type in loadedTypes)
                    {
                        try
                        {
                            if (type != null && IsMigrationType(type, namespaceFilter, nestedNamespaces))
                            {
                                migrationTypes.Add(type);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
            }

            return migrationTypes.Distinct().ToArray();
        }

        private static bool IsMigrationType(Type type, string? namespaceFilter, bool nestedNamespaces)
        {
            try
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    return false;

                if (!typeof(Migration).IsAssignableFrom(type))
                    return false;

                var hasAttribute = type.GetCustomAttributes(typeof(MigrationAttribute), false).Any();
                if (!hasAttribute)
                    return false;

                if (!string.IsNullOrEmpty(namespaceFilter))
                {
                    if (type.Namespace == null)
                        return false;

                    if (nestedNamespaces)
                    {
                        if (!type.Namespace.StartsWith(namespaceFilter, StringComparison.Ordinal))
                            return false;
                    }
                    else
                    {
                        if (!type.Namespace.Equals(namespaceFilter, StringComparison.Ordinal))
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
