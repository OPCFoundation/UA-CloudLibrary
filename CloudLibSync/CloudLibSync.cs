using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library.Client;
using Opc.Ua.Export;

namespace Opc.Ua.CloudLib.Sync
{
    /// <summary>
    /// Sync, Upload and download nodeset between cloud libraries
    /// </summary>
    public class CloudLibSync
    {
        /// <summary>
        /// Create a CloudLibSync instance
        /// </summary>
        /// <param name="logger"></param>
        public CloudLibSync(ILogger logger)
        {
            _logger = logger;
        }
        private readonly ILogger _logger;
        /// <summary>
        /// Downloads node sets from a cloud library to a local directory
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="sourceUserName"></param>
        /// <param name="sourcePassword"></param>
        /// <param name="localDir"></param>
        /// <param name="nodeSetXmlDir"></param>
        /// <returns></returns>
        public async Task DownloadAsync(string sourceUrl, string sourceUserName, string sourcePassword, string localDir, string nodeSetXmlDir)
        {
            var sourceClient = new UACloudLibClient(sourceUrl, sourceUserName, sourcePassword);

            GraphQlResult<Nodeset> nodeSetResult;
            string? cursor = null;
            do
            {
                // Get all NodeSets
                nodeSetResult = await sourceClient.GetNodeSetsAsync(after: cursor, first: 50).ConfigureAwait(false);

                foreach (var nodeSetAndCursor in nodeSetResult.Edges)
                {
                    // Download each NodeSet
                    var identifier = nodeSetAndCursor.Node.Identifier.ToString(CultureInfo.InvariantCulture);
                    var uaNamespace = await sourceClient.DownloadNodesetAsync(identifier).ConfigureAwait(false);

                    if (uaNamespace?.Nodeset != null)
                    {
                        if (!Directory.Exists(localDir))
                        {
                            Directory.CreateDirectory(localDir);
                        }


                        var original = JsonConvert.SerializeObject(uaNamespace, Formatting.Indented);
                        var namespaceKey = VerifyAndFixupNodeSetMeta(uaNamespace);
                        var fileName = GetFileNameForNamespaceUri(namespaceKey.ModelUri, namespaceKey.PublicationDate);

                        File.WriteAllText(Path.Combine(localDir, $"{fileName}.{identifier}.json"), JsonConvert.SerializeObject(uaNamespace, Formatting.Indented));

                        if (namespaceKey.Changed)
                        {
                            if (!Directory.Exists(Path.Combine(localDir, "Original")))
                            {
                                Directory.CreateDirectory(Path.Combine(localDir, "Original"));
                            }
                            File.WriteAllText(Path.Combine(localDir, "Original", $"{fileName}.{identifier}.json"), original);
                        }
                        _logger.LogInformation($"Downloaded {namespaceKey.ModelUri} {namespaceKey.PublicationDate}, {identifier}");

                        if (nodeSetXmlDir != null)
                        {
                            SaveNodeSetAsXmlFile(uaNamespace, nodeSetXmlDir);
                        }
                    }
                }
                cursor = nodeSetResult.PageInfo.EndCursor;
            }
            while (nodeSetResult.PageInfo.HasNextPage);
        }

        /// <summary>
        /// Synchronizes from one Cloud Library to another.
        /// </summary>
        /// <param name="sourceUrl"></param>
        /// <param name="sourceUserName"></param>
        /// <param name="sourcePassword"></param>
        /// <param name="targetUrl"></param>
        /// <param name="targetUserName"></param>
        /// <param name="targetPassword"></param>
        /// <returns></returns>
        public async Task SynchronizeAsync(string sourceUrl, string sourceUserName, string sourcePassword, string targetUrl, string targetUserName, string targetPassword)
        {
            var sourceClient = new UACloudLibClient(sourceUrl, sourceUserName, sourcePassword);
            var targetClient = new UACloudLibClient(targetUrl, targetUserName, targetPassword);

            bool bAdded;
            do
            {
                List<Nodeset> targetNodesets = new();
                GraphQlResult<Nodeset> targetNodeSetResult;
                string? targetCursor = null;
                do
                {
                    targetNodeSetResult = await targetClient.GetNodeSetsAsync(after: targetCursor, first: 50).ConfigureAwait(false);
                    targetNodesets.AddRange(targetNodeSetResult.Edges.Select(e => e.Node));
                    targetCursor = targetNodeSetResult.PageInfo.EndCursor;
                } while (targetNodeSetResult.PageInfo.HasNextPage);

                targetCursor = null;
                do
                {
                    targetNodeSetResult = await targetClient.GetNodeSetsPendingApprovalAsync(after: targetCursor, first: 50).ConfigureAwait(false);
                    targetNodesets.AddRange(targetNodeSetResult.Edges.Select(e => e.Node));
                    targetCursor = targetNodeSetResult.PageInfo.EndCursor;
                } while (targetNodeSetResult.PageInfo.HasNextPage);
                bAdded = false;

                GraphQlResult<Nodeset> sourceNodeSetResult;
                string? sourceCursor = null;
                do
                {
                    sourceNodeSetResult = await sourceClient.GetNodeSetsAsync(after: sourceCursor, first: 50).ConfigureAwait(false);

                    // Get the ones that are not already on the target
                    var toSync = sourceNodeSetResult.Edges
                        .Select(e => e.Node)
                        .Where(source => !targetNodesets
                            .Any(target =>
                                source.NamespaceUri?.OriginalString== target.NamespaceUri?.OriginalString
                                && (source.PublicationDate == target.PublicationDate || (source.Identifier != 0 && source.Identifier == target.Identifier))
                        )).ToList();
                    foreach (var nodeSet in toSync)
                    {
                        // Download each NodeSet
                        var identifier = nodeSet.Identifier.ToString(CultureInfo.InvariantCulture);
                        var uaNamespace = await sourceClient.DownloadNodesetAsync(identifier).ConfigureAwait(false);

                        try
                        {
                            VerifyAndFixupNodeSetMeta(uaNamespace);
                            // upload NodeSet to target cloud library
                            var response = await targetClient.UploadNodeSetAsync(uaNamespace).ConfigureAwait(false);
                            if (response.Status == System.Net.HttpStatusCode.OK)
                            {
                                bAdded = true;
                                _logger.LogInformation($"Uploaded {uaNamespace.Nodeset.NamespaceUri}, {identifier}");
                            }
                            else
                            {
                                _logger.LogError($"Error uploading {uaNamespace.Nodeset.NamespaceUri}, {identifier}: {response.Status} {response.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Error uploading {uaNamespace.Nodeset.NamespaceUri}, {identifier}: {ex.Message}");
                        }
                    }
                    sourceCursor = sourceNodeSetResult.PageInfo.EndCursor;
                } while (sourceNodeSetResult.PageInfo.HasNextPage);
            } while (bAdded);
        }

        /// <summary>
        /// Uploads nodesets from a local directory to a Cloud Library
        /// </summary>
        /// <param name="targetUrl"></param>
        /// <param name="targetUserName"></param>
        /// <param name="targetPassword"></param>
        /// <param name="localDir"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task UploadAsync(string targetUrl, string targetUserName, string targetPassword, string localDir, string fileName)
        {
            var targetClient = new UACloudLibClient(targetUrl, targetUserName, targetPassword);

            var filesToUpload = new List<string>();
            if (!string.IsNullOrEmpty(fileName))
            {
                filesToUpload.Add(fileName);
            }
            else
            {
                filesToUpload.AddRange(Directory.GetFiles(localDir));
            }

            foreach (var file in filesToUpload)
            {
                var uploadJson = File.ReadAllText(file);

                var addressSpace = JsonConvert.DeserializeObject<UANameSpace>(uploadJson);
                if (addressSpace == null)
                {
                    _logger.LogInformation($"Error uploading {file}: failed to parse.");
                    continue;
                }
                if (addressSpace.Nodeset == null || string.IsNullOrEmpty(addressSpace.Nodeset.NodesetXml))
                {
                    var xmlFile = Path.Combine(Path.GetDirectoryName(file)??file, Path.GetFileNameWithoutExtension(file) + ".xml");
                    if (File.Exists(xmlFile))
                    {
                        var xml = File.ReadAllText(xmlFile);
                        addressSpace.Nodeset = new Nodeset { NodesetXml = xml };
                    }
                }
                if (addressSpace.Nodeset == null || string.IsNullOrEmpty(addressSpace.Nodeset.NodesetXml))
                {
                    _logger.LogInformation($"Error uploading {file}: no Nodeset found in file.");
                    continue;
                }
                if (addressSpace.Nodeset.RequiredModels != null)
                {
                    addressSpace.Nodeset.RequiredModels = null;
                }
                if (string.IsNullOrEmpty(addressSpace.Title))
                {
                    addressSpace.Title = file;
                }
                if (string.IsNullOrEmpty(addressSpace.Description))
                {
                    addressSpace.Description = file;
                }
                if (string.IsNullOrEmpty(addressSpace.CopyrightText))
                {
                    addressSpace.CopyrightText = file;
                }
                if (string.IsNullOrEmpty(addressSpace.Category?.Name))
                {
                    addressSpace.Category = new Category { Name = file };
                }
                if (string.IsNullOrEmpty(addressSpace.Contributor?.Name))
                {
                    addressSpace.Contributor = new Organisation { Name = file };
                }
                var response = await targetClient.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
                if (response.Status == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Uploaded {addressSpace.Nodeset.NamespaceUri}, {addressSpace.Nodeset.Identifier}");
                }
                else
                {
                    _logger.LogError($"Error uploading {addressSpace.Nodeset.NamespaceUri}, {addressSpace.Nodeset.Identifier}: {response.Status} {response.Message}");
                }
            }
        }

        private (string? ModelUri, DateTime? PublicationDate, bool Changed) VerifyAndFixupNodeSetMeta(UANameSpace uaNamespace)
        {
            bool changed = false;
            var nodeset = uaNamespace.Nodeset;
            var namespaceUri = nodeset?.NamespaceUri?.OriginalString;
            var publicationDate = nodeset?.PublicationDate;

            if (nodeset?.NodesetXml != null)
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(nodeset.NodesetXml)))
                {
                    var nodeSet = UANodeSet.Read(ms);
                    var firstModel = nodeSet.Models?.FirstOrDefault();
                    if (firstModel != null)
                    {
                        if (firstModel.PublicationDateSpecified && firstModel.PublicationDate != DateTime.MinValue && firstModel.PublicationDate != nodeset.PublicationDate)
                        {
                            _logger.LogWarning($"Publication date {nodeset.PublicationDate} in meta data does not match nodeset {firstModel.PublicationDate}. Fixed up.");
                            publicationDate = firstModel.PublicationDate;
                            nodeset.PublicationDate = publicationDate.Value;
                            changed = true;
                        }
                        if (firstModel.Version != nodeset.Version)
                        {
                            _logger.LogWarning($"Version  {nodeset.Version} in meta data does not match nodeset {firstModel.Version}. Fixed up.");
                            nodeset.Version = firstModel.Version;
                            changed = true;
                        }
                        if (nodeSet.LastModifiedSpecified && nodeSet.LastModified != nodeset.LastModifiedDate)
                        {
                            _logger.LogWarning($"Last modified date {nodeset.LastModifiedDate} in meta data does not match nodeset {nodeSet.LastModified}. Fixed up.");
                            nodeset.LastModifiedDate = nodeSet.LastModified;
                            changed = true;
                        }
                        if (namespaceUri == null)
                        {
                            namespaceUri = nodeSet.Models?.FirstOrDefault()?.ModelUri;
                            changed = true;
                        }
                    }
                }
            }
            if (uaNamespace.Nodeset.RequiredModels != null)
            {
                uaNamespace.Nodeset.RequiredModels = null;
            }
            if (string.IsNullOrEmpty(uaNamespace.Title))
            {
                uaNamespace.Title = nodeset?.NamespaceUri.OriginalString ?? "none";
                changed = true;
            }
            if (string.IsNullOrEmpty(uaNamespace.Description))
            {
                uaNamespace.Description = uaNamespace.Title;
                changed = true;
            }
            if (string.IsNullOrEmpty(uaNamespace.CopyrightText))
            {
                uaNamespace.CopyrightText = uaNamespace.Title;
                changed = true;
            }
            if (string.IsNullOrEmpty(uaNamespace.Category?.Name))
            {
                uaNamespace.Category = new Category { Name = uaNamespace.Title };
                changed = true;
            }
            if (string.IsNullOrEmpty(uaNamespace.Contributor?.Name))
            {
                uaNamespace.Contributor = new Organisation { Name = uaNamespace.Title };
                changed = true;
            }

            return (namespaceUri, publicationDate, changed);
        }

        private static string GetFileNameForNamespaceUri(string? modelUri, DateTime? publicationDate)
        {
            var tFile = modelUri?.Replace("http://", "", StringComparison.OrdinalIgnoreCase) ?? "";
            tFile = tFile.Replace('/', '.');
            tFile = tFile.Replace(':', '_');
            if (!tFile.EndsWith(".", StringComparison.Ordinal)) tFile += ".";
            if (publicationDate != null && publicationDate.Value != default)
            {
                if (publicationDate.Value.TimeOfDay == TimeSpan.Zero)
                {
                    tFile = $"{tFile}{publicationDate:yyyy-MM-dd}.";
                }
                else
                {
                    tFile = $"{tFile}{publicationDate:yyyy-MM-dd-HHmmss}.";
                }
            }
            tFile = $"{tFile}NodeSet2.xml";
            return tFile;
        }

        static void SaveNodeSetAsXmlFile(UANameSpace? nameSpace, string directoryPath)
        {
            var modelUri = nameSpace?.Nodeset?.NamespaceUri?.OriginalString;
            var publicationDate = nameSpace?.Nodeset?.PublicationDate;
            if ((modelUri == null || publicationDate == null) && nameSpace?.Nodeset != null)
            {
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(nameSpace.Nodeset.NodesetXml));
                var model = UANodeSet.Read(ms);
                modelUri = model.Models?.FirstOrDefault()?.ModelUri;
                publicationDate = model.Models?.FirstOrDefault()?.PublicationDate;
            }
            string tFile = GetFileNameForNamespaceUri(modelUri, publicationDate);
            string filePath = Path.Combine(directoryPath, tFile);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(filePath, nameSpace?.Nodeset.NodesetXml);
        }
    }
}
