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
            // Get all infomodels
            var nameAndIdentifiers = await sourceClient.GetNamespaceIdsAsync().ConfigureAwait(false);

            foreach (var nameAndIdentifier in nameAndIdentifiers)
            {
                // Download each infomodel
                var addressSpace = await sourceClient.DownloadNodesetAsync(nameAndIdentifier.Identifier).ConfigureAwait(false);

                if (addressSpace?.Nodeset != null)
                {
                    if (!Directory.Exists(localDir))
                    {
                        Directory.CreateDirectory(localDir);
                    }
                    var fileName = GetFileNameForNamespaceUri(nameAndIdentifier.NamespaceUri);
                    File.WriteAllText(Path.Combine(localDir, $"{fileName}.{nameAndIdentifier.Identifier}.xml"), JsonConvert.SerializeObject(addressSpace));
                    _logger.LogInformation($"Downloaded {nameAndIdentifier.NamespaceUri}, {nameAndIdentifier.Identifier}");

                    if (nodeSetXmlDir != null)
                    {
                        SaveNodeSetAsXmlFile(addressSpace, nodeSetXmlDir);
                    }
                }
            }

        }
        /// <summary>
        /// Synchronizes from one cloud lib to another.
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
                bAdded = false;

                var targetNamespaces = await targetClient.GetNameSpacesAsync(100).ConfigureAwait(false);
                //await FillMissingNamespaceUris(targetClient, targetNamespaces).ConfigureAwait(false);
                var sourceNamespaces = await sourceClient.GetNameSpacesAsync(100).ConfigureAwait(false);
                //await FillMissingNamespaceUris(sourceClient, sourceNamespaces).ConfigureAwait(false);

                // Get the ones that not already on the target
                var toSync = sourceNamespaces.Where(source => !targetNamespaces.Any(target =>
                    source.Nodeset.NamespaceUri?.ToString() == target.Nodeset.NamespaceUri?.ToString()
                    && (source.Nodeset.PublicationDate == target.Nodeset.PublicationDate || (source.Nodeset.Identifier != 0 && source.Nodeset.Identifier == target.Nodeset.Identifier))
                    )).ToList();
                foreach (var nameSpace in toSync)
                {
                    // Download each infomodel
                    var nodeSet = await sourceClient.DownloadNodesetAsync(nameSpace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

                    try
                    {
                        // upload infomodel to target cloud library
                        var response = await targetClient.UploadNodeSetAsync(nodeSet).ConfigureAwait(false);
                        if (response.Status == System.Net.HttpStatusCode.OK)
                        {
                            bAdded = true;
                            _logger.LogInformation($"Uploaded {nameSpace.Nodeset.NamespaceUri}, {nameSpace.Nodeset.Identifier}");
                        }
                        else
                        {
                            _logger.LogError($"Error uploading {nameSpace.Nodeset.NamespaceUri}, {nameSpace.Nodeset.Identifier}: {response.Status} {response.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error uploading { nameSpace.Nodeset.NamespaceUri}, { nameSpace.Nodeset.Identifier}: {ex.Message}");
                    }
                }
            } while (bAdded);
            //// Get all infomodels from both cloudlibs
            //var nameAndIdentifiersTarget = await targetClient.GetNamespacesAsync().ConfigureAwait(false);
            //var nameAndIdentifiers = await sourceClient.GetNamespacesAsync().ConfigureAwait(false);

            //// Get the ones that not already on the target
            //var toSync = nameAndIdentifiers.Where(source => !nameAndIdentifiersTarget.Any(target => source.namespaceUri == target.namespaceUri && source.identifier == target.identifier)).ToList();

            //foreach (var nameAndIdentifier in toSync)
            //{
            //    // Download each infomodel
            //    var addressSpace = await sourceClient.DownloadNodesetAsync(nameAndIdentifier.identifier).ConfigureAwait(false);

            //    // upload infomodel to target cloud library
            //    var error = await targetClient.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
            //    if (string.IsNullOrEmpty(error))
            //    {
            //        _logger.LogInformation($"Uploaded {nameAndIdentifier.namespaceUri}, {nameAndIdentifier.identifier}");
            //    }
            //    else
            //    {
            //        _logger.LogInformation($"Error uploading {nameAndIdentifier.namespaceUri}, {nameAndIdentifier.identifier}: {error}");
            //    }
            //}

        }

        //private static async Task FillMissingNamespaceUris(UACloudLibClient client, List<UANameSpace> nameSpaces)
        //{
        //    if (string.IsNullOrEmpty(nameSpaces.FirstOrDefault()?.Nodeset?.NamespaceUri?.ToString()))
        //    {
        //        var nameAndIdentifiersTarget = await client.GetNamespacesAsync().ConfigureAwait(false);
        //        foreach (var ns in nameSpaces)
        //        {
        //            var ni = nameAndIdentifiersTarget.FirstOrDefault(ni => ni.identifier == ns.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture));
        //            if (!string.IsNullOrEmpty(ni.namespaceUri))
        //            {
        //                ns.Nodeset.NamespaceUri = new Uri(ni.namespaceUri);
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Uploads nodesets from a local directory to a cloud library
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
                var response = await targetClient.UploadNodeSetAsync(addressSpace).ConfigureAwait(false);
                if (response.Status == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation($"Uploaded {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}");
                }
                else
                {
                    _logger.LogError($"Error uploading {addressSpace?.Nodeset.NamespaceUri}, {addressSpace?.Nodeset.Identifier}: {response.Status} {response.Message}");
                }
            }
        }

        private static string GetFileNameForNamespaceUri(string? modelUri)
        {
            var tFile = modelUri?.Replace("http://", "", StringComparison.OrdinalIgnoreCase) ?? "";
            tFile = tFile.Replace('/', '.');
            if (!tFile.EndsWith(".", StringComparison.Ordinal)) tFile += ".";
            tFile = $"{tFile}NodeSet2.xml";
            return tFile;
        }

        static void SaveNodeSetAsXmlFile(UANameSpace? nameSpace, string directoryPath)
        {
            var modelUri = nameSpace?.Nodeset?.NamespaceUri?.ToString();
            if (modelUri == null && nameSpace?.Nodeset != null)
            {
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(nameSpace.Nodeset.NodesetXml));
                var model = UANodeSet.Read(ms);
                modelUri = model.Models?.FirstOrDefault()?.ModelUri;
            }
            string tFile = GetFileNameForNamespaceUri(modelUri);
            string filePath = Path.Combine(directoryPath, tFile);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            File.WriteAllText(filePath, nameSpace?.Nodeset.NodesetXml);
        }
    }
}
