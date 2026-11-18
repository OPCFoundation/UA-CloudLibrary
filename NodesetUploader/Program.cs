using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Client;
using Opc.Ua.Cloud.Client.Models;

namespace NodesetUploader
{
    /// <summary>
    /// Uploads every nodeset (*.xml) found in the source directory, together with its optional values
    /// file, to a UA Cloud Library and publishes (approves) it. Entries already in the library with the
    /// same name (title) as a nodeset being uploaded are deleted first.
    ///
    /// Usage: NodesetUploader [directory] [--dry-run] [--list]
    ///   directory   optional, the folder holding the nodesets and values files. Defaults to the
    ///               NodesetUploader project folder when run from its build output (e.g. dotnet run),
    ///               otherwise to the application directory.
    ///   --dry-run   lists the library entries that would be deleted and the files that would be
    ///               uploaded, without changing the library.
    ///   --list      writes the generated titles to titles.csv in the source directory and exits.
    ///
    /// Environment variables:
    ///   CLOUDLIB_URL         optional, defaults to https://uacloudlibrary.opcfoundation.org
    ///   CLOUDLIB_USERNAME    user name (must hold the Administrator role to delete and publish)
    ///   CLOUDLIB_PASSWORD    password
    ///   CLOUDLIB_LICENSE     optional, defaults to "MIT"
    ///   CLOUDLIB_COPYRIGHT   optional, defaults to "Copyright (c) OPC Foundation"
    ///
    /// Values file lookup for "Foo.NodeSet2.xml": "Foo.NodeSet2.values.json", then "Foo.values.json".
    /// The values are sent in the upload request body, so their size is not limited by the server's
    /// maximum URL length. Requests rejected by the server's rate limiter (429) are retried.
    /// </summary>
    public static partial class Program
    {
        private sealed record NodesetFile(string Path, string Xml, string ModelUri, string Title, string? ValuesPath);

        public static async Task<int> Main(string[] args)
        {
            string directory = ResolveSourceDirectory(args);
            if (!Directory.Exists(directory))
            {
                Console.Error.WriteLine($"Directory {directory} does not exist.");
                return 1;
            }

            if (args.Contains("--list", StringComparer.OrdinalIgnoreCase))
            {
                return await ListTitlesAsync(directory).ConfigureAwait(false);
            }

            bool dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);

            string url = Environment.GetEnvironmentVariable("CLOUDLIB_URL") is { Length: > 0 } envUrl ? envUrl : "https://uacloudlibrary.opcfoundation.org";
            string? username = Environment.GetEnvironmentVariable("CLOUDLIB_USERNAME");
            string? password = Environment.GetEnvironmentVariable("CLOUDLIB_PASSWORD");
            string license = Environment.GetEnvironmentVariable("CLOUDLIB_LICENSE") ?? "MIT";
            string copyright = Environment.GetEnvironmentVariable("CLOUDLIB_COPYRIGHT") ?? "Copyright (c) OPC Foundation";

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Console.Error.WriteLine("Please set the CLOUDLIB_USERNAME and CLOUDLIB_PASSWORD environment variables.");
                return 1;
            }

            List<NodesetFile> nodesets = await LoadNodesetsAsync(directory).ConfigureAwait(false);
            if (nodesets.Count == 0)
            {
                Console.WriteLine($"No nodeset files found in {directory}.");
                return 0;
            }

            // Existing entries are matched by title, so two local nodesets with the same title would
            // each delete the entry the other one just created.
            var duplicateTitles = nodesets.GroupBy(n => n.Title, StringComparer.Ordinal).Where(g => g.Count() > 1).ToList();
            if (duplicateTitles.Count > 0)
            {
                foreach (var duplicate in duplicateTitles)
                {
                    Console.Error.WriteLine($"Title '{duplicate.Key}' is generated for more than one nodeset: {string.Join(", ", duplicate.Select(n => Path.GetFileName(n.Path)))}");
                }

                return 1;
            }

            // The retry handler waits for the server's rate-limit window to reset, which can take up to
            // a minute per attempt, so the default 100 second timeout would cut retries short.
            var httpClient = new HttpClient(new RateLimitRetryHandler(new HttpClientHandler())) {
                BaseAddress = new Uri(url.TrimEnd('/') + "/"),
                Timeout = TimeSpan.FromMinutes(15)
            };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

            // Disposing the cloud library client also disposes the HttpClient.
            using var cloudLib = new UACloudLibClient(httpClient);

            Console.WriteLine($"{(dryRun ? "Dry run: " : string.Empty)}{nodesets.Count} nodesets in {directory}, cloud library {httpClient.BaseAddress}");

            List<(string Title, string Identifier)>? existing = await GetExistingNamesAsync(httpClient).ConfigureAwait(false);
            if (existing == null)
            {
                return 1;
            }

            var titles = new HashSet<string>(nodesets.Select(n => n.Title), StringComparer.Ordinal);
            List<(string Title, string Identifier)> toDelete = existing.Where(e => titles.Contains(e.Title)).ToList();
            Console.WriteLine($"{toDelete.Count} existing entries have the same name as a nodeset to upload.");

            int deleted = 0;
            int failures = 0;
            var notDeleted = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string title, string identifier) in toDelete)
            {
                if (dryRun)
                {
                    Console.WriteLine($"Would delete '{title}' ({identifier}).");
                    continue;
                }

                try
                {
                    if (await DeleteAsync(httpClient, title, identifier).ConfigureAwait(false))
                    {
                        deleted++;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Deleting '{title}' ({identifier}) failed: {ex.Message}");
                }

                failures++;
                notDeleted.Add(title);
            }

            int uploaded = 0;
            int skipped = 0;
            foreach (NodesetFile nodeset in nodesets)
            {
                string fileName = Path.GetFileName(nodeset.Path);
                if (notDeleted.Contains(nodeset.Title))
                {
                    Console.Error.WriteLine($"{fileName}: skipped, because an existing entry named '{nodeset.Title}' could not be deleted.");
                    skipped++;
                    continue;
                }

                if (dryRun)
                {
                    Console.WriteLine($"Would upload {fileName} as '{nodeset.Title}'{(nodeset.ValuesPath != null ? $" with values from {Path.GetFileName(nodeset.ValuesPath)}" : " without values")}.");
                    continue;
                }

                try
                {
                    if (await UploadAndPublishAsync(cloudLib, httpClient, nodeset, license, copyright).ConfigureAwait(false))
                    {
                        uploaded++;
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{fileName}: {ex.Message}");
                }

                failures++;
            }

            if (dryRun)
            {
                Console.WriteLine($"Dry run done. {toDelete.Count} entries would be deleted and {nodesets.Count} nodesets uploaded.");
                return 0;
            }

            Console.WriteLine($"Done. {deleted} deleted, {uploaded} uploaded and published, {skipped} skipped, {failures} failed.");
            return failures == 0 ? 0 : 1;
        }

        // Defaults to the project folder when run from the build output, so the nodesets kept next to
        // the project are found without copying them into bin.
        private static string ResolveSourceDirectory(string[] args)
        {
            string? argument = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal));
            if (argument != null)
            {
                return Path.GetFullPath(argument);
            }

            for (DirectoryInfo? current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "NodesetUploader.csproj")))
                {
                    return current.FullName;
                }
            }

            return AppContext.BaseDirectory;
        }

        private static async Task<List<NodesetFile>> LoadNodesetsAsync(string directory)
        {
            var nodesets = new List<NodesetFile>();
            foreach (string file in Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly)
                .Where(IsNodeset)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                string xml = await File.ReadAllTextAsync(file).ConfigureAwait(false);
                string modelUri = GetModelUri(xml) ?? Path.GetFileNameWithoutExtension(file);
                nodesets.Add(new NodesetFile(file, xml, modelUri, GetTitle(modelUri), FindValuesFile(file)));
            }

            return nodesets;
        }

        // Administrators see every entry, including unpublished submissions.
        private static async Task<List<(string Title, string Identifier)>?> GetExistingNamesAsync(HttpClient client)
        {
            using HttpResponseMessage response = await client.GetAsync("infomodel/names").ConfigureAwait(false);
            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"Listing the existing nodesets failed ({(int)response.StatusCode}): {body}");
                return null;
            }

            // Each entry is "Title,Identifier". The title may itself contain commas, the identifier cannot.
            return (JsonConvert.DeserializeObject<string[]>(body) ?? [])
                .Select(entry => (Entry: entry, Comma: entry.LastIndexOf(',')))
                .Where(e => e.Comma > 0)
                .Select(e => (Title: e.Entry[..e.Comma], Identifier: e.Entry[(e.Comma + 1)..].Trim()))
                .DistinctBy(e => e.Identifier)
                .ToList();
        }

        private static async Task<bool> DeleteAsync(HttpClient client, string title, string identifier)
        {
            using HttpResponseMessage response = await client.DeleteAsync($"infomodel/delete/{Uri.EscapeDataString(identifier)}").ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"Deleted '{title}' ({identifier}).");
                return true;
            }

            string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.Error.WriteLine($"Deleting '{title}' ({identifier}) failed ({(int)response.StatusCode}): {body}");
            return false;
        }

        private static async Task<bool> UploadAndPublishAsync(UACloudLibClient cloudLib, HttpClient client, NodesetFile nodeset, string license, string copyright)
        {
            string fileName = Path.GetFileName(nodeset.Path);

            var nameSpace = new UANameSpace {
                Title = nodeset.Title,
                License = license,
                CopyrightText = copyright,
                Description = $"Information model {nodeset.ModelUri}",
            };
            nameSpace.Nodeset.NodesetXml = nodeset.Xml;

            string values = nodeset.ValuesPath != null ? await File.ReadAllTextAsync(nodeset.ValuesPath).ConfigureAwait(false) : string.Empty;

            // Sends the values in the request body rather than the query string.
            (HttpStatusCode status, string message) = await cloudLib.UploadNodeSetAsync(nameSpace, values, overwrite: true).ConfigureAwait(false);
            if (status != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"{fileName}: upload failed ({(int)status}): {message}");
                return false;
            }

            string identifier = message.Trim().Trim('"');
            Console.WriteLine($"{fileName}: uploaded as {identifier}{(nodeset.ValuesPath != null ? $" with values from {Path.GetFileName(nodeset.ValuesPath)}" : string.Empty)}.");

            string approvalUri = $"approval/{Uri.EscapeDataString(identifier)}?name={Uri.EscapeDataString(nameSpace.Title)}";
            using HttpResponseMessage approvalResponse = await client.PutAsync(approvalUri, null).ConfigureAwait(false);
            string approvalBody = await approvalResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (approvalResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"{fileName}: publish failed ({(int)approvalResponse.StatusCode}): {approvalBody}");
                return false;
            }

            Console.WriteLine($"{fileName}: published.");
            return true;
        }

        // "http://catena-x.org/UA/urn_samm_io_catenax_pcf_7_0_0_Pcf/" -> "Pcf 7.0.0"
        private static string GetTitle(string modelUri)
        {
            string segment = modelUri.TrimEnd('/');
            segment = segment[(segment.LastIndexOf('/') + 1)..];

            // "http://opcfoundation.org/UA/BatteryPassport/EV/2027-02/" -> "EV Battery Passport 2027-02"
            if (VersionSegment().IsMatch(segment))
            {
                string[] path = new Uri(modelUri).AbsolutePath.Trim('/').Split('/');
                string[] words = path[..^1].Where(s => !s.Equals("UA", StringComparison.OrdinalIgnoreCase)).Reverse()
                    .Select(s => string.Join(' ', CamelCaseBoundary().Split(s))).ToArray();
                return words.Length > 0 ? $"{string.Join(' ', words)} {segment}" : segment;
            }

            string[] parts = segment.Split('_');
            string name = parts[^1];
            if (string.IsNullOrWhiteSpace(name))
            {
                return modelUri;
            }

            var version = new Stack<string>();
            int i = parts.Length - 2;
            for (; i >= 0 && parts[i].Length > 0 && parts[i].All(char.IsDigit); i--)
            {
                version.Push(parts[i]);
            }

            // "industry_core_batch_4_0_0_Batch" -> prefix "Industry Core"
            string[] nameWords = CamelCaseBoundary().Split(name).Select(w => w.ToLowerInvariant()).ToArray();
            int end = i + 1;
            int start = end - nameWords.Length;
            string prefix = string.Empty;
            if (start >= 0 && parts[start..end].Select(p => p.ToLowerInvariant()).SequenceEqual(nameWords))
            {
                int catenax = Array.FindLastIndex(parts, start, p => p.Equals("catenax", StringComparison.OrdinalIgnoreCase));
                string[] prefixWords = parts[(catenax + 1)..start];
                prefix = string.Join(' ', prefixWords.Select(w => w.Length > 0 ? char.ToUpperInvariant(w[0]) + w[1..] : w));
            }

            string title = prefix.Length > 0 ? $"{prefix} {name}" : name;
            return version.Count > 0 ? $"{title} {string.Join('.', version)}" : title;
        }

        [System.Text.RegularExpressions.GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
        private static partial System.Text.RegularExpressions.Regex CamelCaseBoundary();

        [System.Text.RegularExpressions.GeneratedRegex(@"^\d[\d.\-]*$")]
        private static partial System.Text.RegularExpressions.Regex VersionSegment();

        private static async Task<int> ListTitlesAsync(string directory)
        {
            List<NodesetFile> nodesets = await LoadNodesetsAsync(directory).ConfigureAwait(false);

            var lines = new List<string> { "Title,ModelUri,File" };
            lines.AddRange(nodesets.Select(n => $"\"{n.Title}\",\"{n.ModelUri}\",\"{Path.GetFileName(n.Path)}\""));

            string output = Path.Combine(directory, "titles.csv");
            await File.WriteAllLinesAsync(output, lines).ConfigureAwait(false);
            Console.WriteLine($"Wrote {nodesets.Count} titles to {output}");
            return 0;
        }

        private static string? GetModelUri(string xml)
        {
            return XDocument.Parse(xml).Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Models")
                ?.Elements().FirstOrDefault(e => e.Name.LocalName == "Model")?.Attribute("ModelUri")?.Value;
        }

        private static bool IsNodeset(string file)
        {
            try
            {
                using var reader = System.Xml.XmlReader.Create(file);
                reader.MoveToContent();
                return reader.LocalName == "UANodeSet";
            }
            catch (System.Xml.XmlException)
            {
                return false;
            }
        }

        private static string? FindValuesFile(string nodesetFile)
        {
            string directory = Path.GetDirectoryName(nodesetFile)!;
            string stem = Path.GetFileNameWithoutExtension(nodesetFile);

            var candidates = new List<string> { Path.Combine(directory, stem + ".values.json") };
            if (stem.EndsWith(".NodeSet2", StringComparison.OrdinalIgnoreCase))
            {
                candidates.Add(Path.Combine(directory, stem[..^".NodeSet2".Length] + ".values.json"));
            }

            return candidates.FirstOrDefault(File.Exists);
        }
    }
}
