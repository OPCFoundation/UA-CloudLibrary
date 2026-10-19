using System;
using System.Collections.Generic;
using System.IO;

namespace CloudLibClient.Tests
{
    /// <summary>
    /// Minimal reader for the repository's docker-compose <c>.env</c> file so local test runs can
    /// pick up the same database settings docker-compose uses, without duplicating them.
    /// Mirrors compose semantics: the file lives next to <c>docker-compose.yml</c>, lines are
    /// <c>KEY=VALUE</c>, <c>#</c> starts a comment, surrounding single/double quotes are stripped,
    /// and a variable already present in the process environment takes precedence over the file.
    /// </summary>
    internal static class DotEnvFile
    {
        private static readonly Lazy<Dictionary<string, string>> s_values = new(Load);

        /// <summary>
        /// Returns the value of <paramref name="key"/> from the process environment, falling back to
        /// the repository <c>.env</c> file; <c>null</c> when neither defines it.
        /// </summary>
        public static string Get(string key)
        {
            string fromProcess = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromProcess))
            {
                return fromProcess;
            }

            return s_values.Value.TryGetValue(key, out string fromFile) && !string.IsNullOrWhiteSpace(fromFile)
                ? fromFile
                : null;
        }

        private static Dictionary<string, string> Load()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string path = FindEnvFile();
            if (path == null)
            {
                return values;
            }

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                if (line.StartsWith("export ", StringComparison.Ordinal))
                {
                    line = line.Substring("export ".Length).TrimStart();
                }

                int separator = line.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();

                if (value.Length >= 2 &&
                    ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
                {
                    value = value.Substring(1, value.Length - 2);
                }
                else
                {
                    // Unquoted values may carry a trailing inline comment.
                    int comment = value.IndexOf(" #", StringComparison.Ordinal);
                    if (comment >= 0)
                    {
                        value = value.Substring(0, comment).TrimEnd();
                    }
                }

                values[key] = value;
            }

            return values;
        }

        // Walk up from the test output directory to the directory containing docker-compose.yml,
        // which is where docker-compose itself looks for .env.
        private static string FindEnvFile()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
                {
                    string candidate = Path.Combine(directory.FullName, ".env");
                    return File.Exists(candidate) ? candidate : null;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
