using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Fails the build if any value-set change can happen without an audit entry.
    /// </summary>
    /// <remarks>
    /// A value set is a Digital Product Passport, so every change to one must be audited. That is
    /// enforced by routing each change through an audit helper; the risk is a call site that bypasses
    /// it. This scans the sources that may change value sets and requires each store mutation to sit
    /// inside an audit helper call. The store itself and DPPService are excluded on purpose: the store
    /// is what the helpers wrap, and the PATCH path is audited by its controller around the update.
    /// </remarks>
    public partial class ValueSetAuditCoverageTests
    {
        private static readonly string[] s_mutations =
        {
            "CreateAsync", "UpdateAsync", "DeleteAsync", "CloneAsync", "SetPublishedAsync",
            "SetValueAsync", "SetValuesAsync", "RemoveValueAsync", "RestoreValuesAsync",
        };

        private static readonly string[] s_helpers = { "AuditedAsync(", "AuditedValueSetChangeAsync(" };

        [GeneratedRegex(@"\b(?:_store|ValueSetStore)\.(\w+)\(")]
        private static partial Regex StoreCallRegex();

        [Fact]
        public void EveryValueSetMutation_IsAudited()
        {
            var offenders = new List<string>();
            int audited = 0;

            foreach (string file in SourceFiles())
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match m in StoreCallRegex().Matches(lines[i]))
                    {
                        if (!s_mutations.Contains(m.Groups[1].Value, StringComparer.Ordinal))
                        {
                            continue;
                        }

                        // Each helper call wraps its change in a lambda on the same line, so a
                        // mutation is covered exactly when its line also invokes a helper.
                        if (s_helpers.Any(h => lines[i].Contains(h, StringComparison.Ordinal)))
                        {
                            audited++;
                        }
                        else
                        {
                            offenders.Add($"{Path.GetFileName(file)}:{i + 1} calls {m.Groups[1].Value} outside an audit helper");
                        }
                    }
                }
            }

            Assert.True(offenders.Count == 0,
                "A value-set change is made without being audited. Route it through AuditedAsync " +
                "(controllers) or AuditedValueSetChangeAsync (Blazor pages):" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders));

            // Guards the guard: a scan that stopped matching (a rename, a moved folder) would pass
            // vacuously. 17 = 9 in ValueSetController + 8 in TreePage.
            Assert.True(audited >= 17, $"Expected at least 17 audited value-set mutations, found {audited}; the scan may have stopped matching.");
        }

        private static IEnumerable<string> SourceFiles()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.GetDirectories("UACloudLibraryServer").Length == 0)
            {
                dir = dir.Parent;
            }

            Assert.NotNull(dir);
            string server = Path.Combine(dir.FullName, "UACloudLibraryServer");

            return Directory.EnumerateFiles(Path.Combine(server, "Controllers"), "*.cs")
                .Concat(Directory.EnumerateFiles(Path.Combine(server, "Components"), "*.razor", SearchOption.AllDirectories));
        }
    }
}
