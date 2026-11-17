using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Client.Models;

namespace NodesetUploader
{
    /// <summary>
    /// Uploads every nodeset (*.xml) found in the application directory, together with its optional
    /// values file, to a UA Cloud Library, overwriting existing entries and publishing (approving) them.
    ///
    /// Environment variables:
    ///   CLOUDLIB_URL         optional, defaults to https://uacloudlibrary.opcfoundation.org
    ///   CLOUDLIB_USERNAME    user name (must hold the Administrator role to publish)
    ///   CLOUDLIB_PASSWORD    password
    ///   CLOUDLIB_LICENSE     optional, defaults to "MIT"
    ///   CLOUDLIB_COPYRIGHT   optional, defaults to "Copyright (c) OPC Foundation"
    ///
    /// Values file lookup for "Foo.NodeSet2.xml": "Foo.NodeSet2.values.json", then "Foo.values.json".
    /// </summary>
    public static partial class Program
    {
        public static async Task<int> Main(string[] args)
        {
            if (args.Contains("--list", StringComparer.OrdinalIgnoreCase))
            {
                return await ListTitlesAsync().ConfigureAwait(false);
            }

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

            using var client = new HttpClient { BaseAddress = new Uri(url.TrimEnd('/') + "/") };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

            string[] nodesetFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                .Where(IsNodeset)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (nodesetFiles.Length == 0)
            {
                Console.WriteLine($"No nodeset files found in {AppContext.BaseDirectory}.");
                return 0;
            }

            int failures = 0;
            foreach (string nodesetFile in nodesetFiles)
            {
                try
                {
                    if (!await UploadAndPublishAsync(client, nodesetFile, license, copyright).ConfigureAwait(false))
                    {
                        failures++;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{Path.GetFileName(nodesetFile)}: {ex.Message}");
                    failures++;
                }
            }

            Console.WriteLine($"Done. {nodesetFiles.Length - failures} succeeded, {failures} failed.");
            return failures == 0 ? 0 : 1;
        }

        private static async Task<bool> UploadAndPublishAsync(HttpClient client, string nodesetFile, string license, string copyright)
        {
            string fileName = Path.GetFileName(nodesetFile);
            string xml = await File.ReadAllTextAsync(nodesetFile).ConfigureAwait(false);

            XElement? model = XDocument.Parse(xml).Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Models")
                ?.Elements().FirstOrDefault(e => e.Name.LocalName == "Model");
            string modelUri = model?.Attribute("ModelUri")?.Value ?? Path.GetFileNameWithoutExtension(nodesetFile);
            string title = GetTitle(modelUri);

            var nameSpace = new UANameSpace {
                Title = title,
                License = license,
                CopyrightText = copyright,
                Description = $"Information model {modelUri}",
            };
            nameSpace.Nodeset.NodesetXml = xml;

            string? valuesFile = FindValuesFile(nodesetFile);
            string values = valuesFile != null ? await File.ReadAllTextAsync(valuesFile).ConfigureAwait(false) : string.Empty;

            string uploadUri = "infomodel/upload?overwrite=true";
            if (!string.IsNullOrWhiteSpace(values))
            {
                uploadUri += "&values=" + Uri.EscapeDataString(values);
            }

            using var content = new StringContent(JsonConvert.SerializeObject(nameSpace), Encoding.UTF8, "application/json");
            using HttpResponseMessage uploadResponse = await client.PutAsync(uploadUri, content).ConfigureAwait(false);
            string uploadBody = await uploadResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (uploadResponse.StatusCode != HttpStatusCode.OK)
            {
                Console.Error.WriteLine($"{fileName}: upload failed ({(int)uploadResponse.StatusCode}): {uploadBody}");
                return false;
            }

            string identifier = uploadBody.Trim().Trim('"');
            Console.WriteLine($"{fileName}: uploaded as {identifier}{(valuesFile != null ? $" with values from {Path.GetFileName(valuesFile)}" : string.Empty)}.");

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

        private static async Task<int> ListTitlesAsync()
        {
            string[] nodesetFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly)
                .Where(IsNodeset)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var lines = new List<string> { "Title,ModelUri,File" };
            foreach (string nodesetFile in nodesetFiles)
            {
                string modelUri = GetModelUri(await File.ReadAllTextAsync(nodesetFile).ConfigureAwait(false)) ?? Path.GetFileNameWithoutExtension(nodesetFile);
                lines.Add($"\"{GetTitle(modelUri)}\",\"{modelUri}\",\"{Path.GetFileName(nodesetFile)}\"");
            }

            string output = Path.Combine(AppContext.BaseDirectory, "titles.csv");
            await File.WriteAllLinesAsync(output, lines).ConfigureAwait(false);
            Console.WriteLine($"Wrote {nodesetFiles.Length} titles to {output}");
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
