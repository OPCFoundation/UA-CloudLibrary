using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Guards the post-commit audit contract.
    /// </summary>
    /// <remarks>
    /// A mutation that has already been applied must not report an audit failure as a retryable
    /// refusal, because the client would repeat a change that is already durable. The distinction is
    /// carried by <see cref="DppAuditException.MutationCommitted"/> and can only be set at the call
    /// site that knows the write succeeded, so nothing at runtime can detect a site that simply
    /// forgot to use <see cref="DppAuditLogExtensions.RecordCommittedOutcomeAsync"/>. The source
    /// scan below is therefore the test: it fails when a post-mutation outcome is recorded through
    /// the plain method, which is exactly how this class of bug returned after it was first fixed.
    /// </remarks>
    public partial class DppAuditCommittedOutcomeTests
    {
        /// <summary>Outcomes that can only be reached once a change is durably applied.</summary>
        private static readonly string[] s_postCommitOutcomes = { "Success", "PartialFailure" };

        [GeneratedRegex("\"(Success|PartialFailure)\"")]
        private static partial Regex PostCommitOutcomeRegex();

        private sealed class ThrowingAuditLog : IDppAuditLog
        {
            public Task RecordAsync(string operatorId, DppAuditOperation operation, string dppId, string elementPath, string outcome, string operationId = null) =>
                throw new DppAuditException("append failed");

            public Task<DppAuditVerificationOutcome> VerifyChainAsync(System.Threading.CancellationToken cancellationToken = default) =>
                Task.FromResult(DppAuditVerificationOutcome.Verified);
        }

        [Fact]
        public async Task RecordCommittedOutcomeAsync_MarksTheFailureAsCommitted()
        {
            IDppAuditLog log = new ThrowingAuditLog();

            DppAuditException ex = await Assert.ThrowsAsync<DppAuditException>(
                () => log.RecordCommittedOutcomeAsync("op", DppAuditOperation.Modify, "dpp-1", null, "Success", "oid"));

            Assert.True(ex.MutationCommitted);
        }

        [Fact]
        public async Task RecordAsync_LeavesTheFailureUnmarked()
        {
            // The default must stay false so a site that says nothing gets the safe, retryable
            // interpretation rather than silently claiming a change was applied.
            IDppAuditLog log = new ThrowingAuditLog();

            DppAuditException ex = await Assert.ThrowsAsync<DppAuditException>(
                () => log.RecordAsync("op", DppAuditOperation.Modify, "dpp-1", null, "Success", "oid"));

            Assert.False(ex.MutationCommitted);
        }

        [Fact]
        public void EveryPostMutationOutcome_UsesTheCommittedHelper()
        {
            string controllers = ControllersDirectory();
            var offenders = new List<string>();

            foreach (string file in Directory.EnumerateFiles(controllers, "*.cs"))
            {
                string source = File.ReadAllText(file);

                foreach (AuditInvocation call in FindAuditInvocations(source))
                {
                    // Read outcomes are legitimately retryable: nothing was mutated.
                    if (call.Text.Contains("DppAuditOperation.Read", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    Match outcome = PostCommitOutcomeRegex().Match(call.Text);
                    if (!outcome.Success)
                    {
                        continue;
                    }

                    if (!call.Text.Contains("RecordCommittedOutcomeAsync", StringComparison.Ordinal))
                    {
                        int line = source.Take(call.Start).Count(c => c == '\n') + 1;
                        offenders.Add($"{Path.GetFileName(file)}:{line} records \"{outcome.Groups[1].Value}\" via RecordAsync");
                    }
                }
            }

            Assert.True(
                offenders.Count == 0,
                "A post-mutation audit outcome is recorded through RecordAsync, so a failed append would be " +
                "reported as a retryable 503 for a change that is already durable. Use " +
                "RecordCommittedOutcomeAsync instead:" + Environment.NewLine +
                string.Join(Environment.NewLine, offenders));
        }

        private readonly record struct AuditInvocation(int Start, string Text);

        /// <summary>
        /// Extracts each complete <c>_auditLog.Record...(...)</c> invocation, argument list included.
        /// </summary>
        /// <remarks>
        /// Whole invocations rather than single lines. An earlier version matched the call and its
        /// outcome argument on the same physical line, so a multiline call - of which several exist
        /// in these controllers - could be switched from <c>RecordCommittedOutcomeAsync</c> to
        /// <c>RecordAsync</c> while its <c>"PartialFailure"</c> argument sat on a later line, and the
        /// scan would never inspect the two together. The invariant was effectively unenforced for
        /// exactly the calls most likely to carry it.
        /// <para>
        /// Parenthesis balancing rather than a real parse: it is string- and comment-aware, which is
        /// sufficient for a guard over first-party controller source and avoids taking a Roslyn
        /// dependency in this test project.
        /// </para>
        /// </remarks>
        private static IEnumerable<AuditInvocation> FindAuditInvocations(string source)
        {
            const string marker = "_auditLog.Record";

            int index = source.IndexOf(marker, StringComparison.Ordinal);
            while (index >= 0)
            {
                int open = source.IndexOf('(', index);
                if (open < 0)
                {
                    yield break;
                }

                int close = FindMatchingParen(source, open);
                if (close < 0)
                {
                    yield break;
                }

                yield return new AuditInvocation(index, source[index..(close + 1)]);
                index = source.IndexOf(marker, close, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Index of the parenthesis closing the one at <paramref name="open"/>, ignoring parentheses
        /// inside string literals and comments. Returns -1 when unbalanced.
        /// </summary>
        private static int FindMatchingParen(string source, int open)
        {
            int depth = 0;

            for (int i = open; i < source.Length; i++)
            {
                char c = source[i];

                // Skip over a string or character literal wholesale, so a parenthesis or quote
                // inside a message does not unbalance the count.
                if (c is '"' or '\'')
                {
                    i = SkipLiteral(source, i);
                    continue;
                }

                if (c == '/' && i + 1 < source.Length)
                {
                    if (source[i + 1] == '/')
                    {
                        int eol = source.IndexOf('\n', i);
                        if (eol < 0)
                        {
                            return -1;
                        }

                        i = eol;
                        continue;
                    }

                    if (source[i + 1] == '*')
                    {
                        int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                        if (end < 0)
                        {
                            return -1;
                        }

                        i = end + 1;
                        continue;
                    }
                }

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>Index of the closing quote of the literal starting at <paramref name="start"/>.</summary>
        private static int SkipLiteral(string source, int start)
        {
            char quote = source[start];

            // Verbatim strings have no escape sequences; a doubled quote is an escaped quote.
            bool verbatim = start > 0 && source[start - 1] == '@';

            for (int i = start + 1; i < source.Length; i++)
            {
                char c = source[i];

                if (!verbatim && c == '\\')
                {
                    i++;
                    continue;
                }

                if (c == quote)
                {
                    if (verbatim && i + 1 < source.Length && source[i + 1] == quote)
                    {
                        i++;
                        continue;
                    }

                    return i;
                }
            }

            return source.Length - 1;
        }

        [Fact]
        public void PostCommitOutcomeList_MatchesTheScannedPattern()
        {
            // Keeps the documented vocabulary and the scan's pattern from drifting apart. If a new
            // terminal outcome implying durable change is added, both have to be updated together.
            foreach (string outcome in s_postCommitOutcomes)
            {
                Assert.Matches(PostCommitOutcomeRegex(), $"\"{outcome}\"");
            }

            Assert.DoesNotMatch(PostCommitOutcomeRegex(), "\"Failed\"");
            Assert.DoesNotMatch(PostCommitOutcomeRegex(), "\"Attempted\"");
        }

        private static string ControllersDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && dir.GetDirectories("UACloudLibraryServer").Length == 0)
            {
                dir = dir.Parent;
            }

            Assert.NotNull(dir);
            string controllers = Path.Combine(dir.FullName, "UACloudLibraryServer", "Controllers");
            Assert.True(Directory.Exists(controllers), $"Controllers directory not found at '{controllers}'.");
            return controllers;
        }
    }
}
