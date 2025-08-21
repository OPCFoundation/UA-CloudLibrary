/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AdminShell;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;
using static NpgsqlTypes.NpgsqlTsQuery;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibDataProvider
    {
        private readonly AppDbContext _dbContext = null;
        private readonly DbFileStorage _storage;
        private readonly ILogger _logger;
        private readonly bool _approvalRequired;

        public CloudLibDataProvider(AppDbContext context, ILoggerFactory logger, DbFileStorage storage, IConfiguration configuration)
        {
            _dbContext = context;
            _storage = storage;
            _logger = logger.CreateLogger("CloudLibDataProvider");
            _approvalRequired = configuration.GetSection("CloudLibrary")?.GetValue<bool>("ApprovalRequired") ?? false;
        }

        public List<NodesetViewerNode> GetNodeListOfType(string strUserId, string strType, string strDefault, string strUri = null)
        {
            List<NodesetViewerNode> result = new();

            bool bIgnoreUri = string.IsNullOrEmpty(strUri);

            var allNodes = GetNodeSets(strUserId);
            foreach (var nodeset in allNodes)
            {
                NodesetViewerNode nswn = new();
                foreach (var xx in nodeset.Objects)
                {
                    if (xx.DisplayName[0].Text == strType)
                    {
                        if (bIgnoreUri || xx.NodeId.Contains(strUri, StringComparison.CurrentCulture))
                        {
                            nswn.Id = xx.NodeId;
                            nswn.Value = strDefault;
                            nswn.Text = xx.DisplayName[0].Text;
                            result.Add(nswn);
                        }
                    }
                }
            }

            return result;
        }


        public IQueryable<NodeSetModel> GetNodeSets(
            string userId,
            string identifier = null,
            string modelUri = null,
            DateTime? publicationDate = null,
            string[] keywords = null)
        {
            IQueryable<NodeSetModel> nodeSets;
            if (!string.IsNullOrEmpty(identifier))
            {
                if (modelUri != null || publicationDate != null || keywords != null)
                {
                    throw new ArgumentException($"Must not specify other parameters when providing identifier.");
                }

                // Return unapproved nodesets only if request by identifier, but not in queries
                nodeSets = _dbContext.NodeSetsWithUnapproved.AsQueryable().Where(nsm => nsm.Identifier == identifier);
            }
            else
            {
                IQueryable<NodeSetModel> nodeSetQuery = SearchNodesets(keywords, userId);
                if (modelUri != null && publicationDate != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == modelUri && nsm.PublicationDate == publicationDate);
                }
                else if (modelUri != null)
                {
                    nodeSets = nodeSetQuery.Where(nsm => nsm.ModelUri == modelUri);
                }
                else
                {
                    nodeSets = nodeSetQuery;
                }
            }

            return nodeSets;
        }

        public IQueryable<NamespaceMetaDataModel> NamespaceMetaData
        {
            get => _approvalRequired
                ? _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.ApprovalStatus == ApprovalStatus.Approved)
                : _dbContext.NamespaceMetaDataWithUnapproved;
        }

        public IQueryable<NodeSetModel> NodeSets
        {
            get =>
                _approvalRequired
                ? _dbContext.NodeSetsWithUnapproved.Where(n => _dbContext.NamespaceMetaDataWithUnapproved.Any(nmd => nmd.NodesetId == n.Identifier && nmd.ApprovalStatus == ApprovalStatus.Approved))
                : _dbContext.NodeSetsWithUnapproved;
        }

        private IQueryable<T> GetNodeModels<T>(Expression<Func<NodeSetModel, IEnumerable<T>>> selector, string userId, string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
            where T : NodeModel
        {
            if (nodeId != null && modelUri == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    modelUri = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeSetModel> nodeSets;
            if (modelUri != null && publicationDate != null)
            {
                nodeSets = NodeSets.AsQueryable().Where(nsm => (nsm.ModelUri == modelUri) && (nsm.PublicationDate == publicationDate) && ((userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId)));
            }
            else if (modelUri != null)
            {
                nodeSets = NodeSets.AsQueryable().Where(nsm => (nsm.ModelUri == modelUri) && ((userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId)));
            }
            else
            {
                nodeSets = NodeSets.AsQueryable().Where(nsm => (userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId));
            }

            IQueryable<T> nodeModels = nodeSets.SelectMany(selector);
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        public string GetIdentifier(UANameSpace uaNamespace)
        {
            UANodeSet nodeSet = null;

            try
            {
                nodeSet = ReadUANodeSet(uaNamespace.Nodeset.NodesetXml);
            }
            catch (Exception ex)
            {
                return $"Could not parse nodeset XML file: {ex.Message}";
            }

            return GenerateHashCode(nodeSet).ToString(CultureInfo.InvariantCulture);
        }

        public async Task<string> UploadNamespaceAndNodesetAsync(UANameSpace uaNamespace, string values, bool overwrite, string userId)
        {
            UANodeSet nodeSet = null;

            try
            {
                nodeSet = ReadUANodeSet(uaNamespace.Nodeset.NodesetXml);
            }
            catch (Exception ex)
            {
                return $"Could not parse nodeset XML file: {ex.Message}";
            }

            // generate a unique hash code
            uint legacyNodesetHashCode;
            uint nodesetHashCode = GenerateHashCode(nodeSet);
            {
                if (nodesetHashCode == 0)
                {
                    return "Nodeset invalid. Please make sure it includes a valid Model URI and publication date!";
                }

                if (nodeSet.Models.Length != 1)
                {
                    return "Nodeset not supported. Please make sure it includes exactly one Model!";
                }

                // check if the nodeset already exists in the database for the legacy hashcode algorithm
                legacyNodesetHashCode = GenerateHashCodeLegacy(nodeSet);
                if (legacyNodesetHashCode != 0)
                {
                    DbFiles legacyNodeSetXml = await _storage.DownloadFileAsync(legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
                    if (legacyNodeSetXml != null)
                    {
                        try
                        {
                            UANodeSet legacyNodeSet = ReadUANodeSet(legacyNodeSetXml.Blob);
                            ModelTableEntry firstModel = legacyNodeSet.Models.Length > 0 ? legacyNodeSet.Models[0] : null;
                            if (firstModel == null)
                            {
                                return $"Nodeset exists but existing nodeset had no model entry.";
                            }
                            if ((!firstModel.PublicationDateSpecified && !nodeSet.Models[0].PublicationDateSpecified) || firstModel.PublicationDate == nodeSet.Models[0].PublicationDate)
                            {
                                if (!overwrite)
                                {
                                    // nodeset already exists
                                    return "Nodeset already exists. Use overwrite flag to overwrite this existing legacy entry in the Library.";
                                }
                            }
                            else
                            {
                                // New nodeset is a different version from the legacy nodeset: don't touch the legacy nodeset
                                legacyNodesetHashCode = 0;
                            }
                        }
                        catch (Exception ex)
                        {
                            return $"Nodeset exists but existing nodeset could not be validated: {ex.Message}.";
                        }

                        // check userId matches if nodeset already exists
                        NamespaceMetaDataModel existingLegacyNamespaces = _dbContext.NamespaceMetaDataWithUnapproved
                            .Where(n => (n.NodesetId == legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture)) && ((userId == "admin") || (n.UserId == userId) || string.IsNullOrEmpty(n.UserId)))
                            .Include(n => n.NodeSet)
                            .FirstOrDefault();

                        if (existingLegacyNamespaces != null)
                        {
                            // we treat no user in the database like an admin user
                            if (string.IsNullOrEmpty(existingLegacyNamespaces.UserId) && (userId != "admin"))
                            {
                                return $"Nodeset already exists for admin user. Cannot overwrite with user {userId}";
                            }
                        }
                    }
                }

                uaNamespace.Nodeset.Identifier = nodesetHashCode;
            }

            // check if the nodeset already exists in the database for the new hashcode algorithm
            string result = await _storage.FindFileAsync(uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(result) && !overwrite)
            {
                UANameSpace existingNamespace = await RetrieveAllMetadataAsync(uaNamespace.Nodeset.Identifier, userId).ConfigureAwait(false);
                if (existingNamespace != null)
                {
                    // nodeset already exists
                    return "Nodeset already exists. Use overwrite flag to overwrite this existing entry in the Library.";
                }

                // nodeset metadata not found: allow overwrite of orphaned blob
                overwrite = true;
            }

            // check userId matches if nodeset already exists
            NamespaceMetaDataModel existingNamespaces = _dbContext.NamespaceMetaDataWithUnapproved
                .Where(n => (n.NodesetId == uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)) && ((userId == "admin") || (n.UserId == userId) || string.IsNullOrEmpty(n.UserId)))
                .Include(n => n.NodeSet)
                .FirstOrDefault();

            if (existingNamespaces != null)
            {
                // we treat no user in the database like an admin user
                if (string.IsNullOrEmpty(existingNamespaces.UserId) && (userId != "admin"))
                {
                    return $"Nodeset already exists for admin user. Cannot overwrite with user {userId}";
                }
            }

            uaNamespace.CreationTime = DateTime.UtcNow;

            if (uaNamespace.Nodeset.PublicationDate != nodeSet.Models[0].PublicationDate)
            {
                if (nodeSet.Models[0].PublicationDate != DateTime.MinValue)
                {
                    _logger.LogInformation("PublicationDate in metadata does not match nodeset XML. Taking nodeset XML publication date.");
                    uaNamespace.Nodeset.PublicationDate = nodeSet.Models[0].PublicationDate;
                }
            }

            if (uaNamespace.Nodeset.Version != nodeSet.Models[0].Version)
            {
                _logger.LogInformation("Version in metadata does not match nodeset XML. Taking nodeset XML version.");
                uaNamespace.Nodeset.Version = nodeSet.Models[0].Version;
            }

            if (uaNamespace.Nodeset.NamespaceUri != null && uaNamespace.Nodeset.NamespaceUri.OriginalString != nodeSet.Models[0].ModelUri)
            {
                _logger.LogInformation("NamespaceUri in metadata does not match nodeset XML. Taking nodeset XML namespaceUri.");
                uaNamespace.Nodeset.NamespaceUri = new Uri(nodeSet.Models[0].ModelUri);
            }

            if (uaNamespace.Nodeset.LastModifiedDate != nodeSet.LastModified)
            {
                if (nodeSet.LastModifiedSpecified && (nodeSet.LastModified != DateTime.MinValue))
                {
                    _logger.LogInformation("LastModifiedDate in metadata does not match nodeset XML. Taking nodeset XML last modified date.");
                    uaNamespace.Nodeset.LastModifiedDate = nodeSet.LastModified;
                }
            }

            // Ignore RequiredModels if provided: cloud library will read from the nodeset
            uaNamespace.Nodeset.RequiredModels = null;

            // At this point all inputs are validated: ready to store

            // upload the new file to the storage service, and get the file handle that the storage service returned
            string storedFilename = await _storage.UploadFileAsync(uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture), uaNamespace.Nodeset.NodesetXml, values).ConfigureAwait(false);
            if (string.IsNullOrEmpty(storedFilename) || (storedFilename != uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)))
            {
                string message = "Error: NodeSet file could not be stored.";
                _logger.LogError(message);
                return message;
            }

            string dbMessage = await AddMetaDataAsync(uaNamespace, nodeSet, legacyNodesetHashCode, userId).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(dbMessage))
            {
                _logger.LogError(dbMessage);
                return dbMessage;
            }

            if (legacyNodesetHashCode != 0)
            {
                try
                {
                    string legacyHashCodeStr = legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture);
                    if (!string.IsNullOrEmpty(await _storage.FindFileAsync(legacyHashCodeStr).ConfigureAwait(false)))
                    {
                        await _storage.DeleteFileAsync(legacyHashCodeStr).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to delete legacy nodeset {legacyNodesetHashCode} for {uaNamespace?.Nodeset?.NamespaceUri} {uaNamespace?.Nodeset?.PublicationDate} {uaNamespace?.Nodeset?.Identifier}");
                }
            }

            await IndexNodeSetModelAsync(nodeSet, uaNamespace).ConfigureAwait(false);

            return "success";
        }

        public static UANodeSet ReadUANodeSet(string nodeSetXml)
        {
            UANodeSet nodeSet;
            // workaround for bug https://github.com/dotnet/runtime/issues/67622
            nodeSetXml = new string(nodeSetXml.Replace("<Value/>", "<Value xsi:nil='true' />", StringComparison.Ordinal).Where(c => c != '\0').ToArray());

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(nodeSetXml)))
            {
                nodeSet = UANodeSet.Read(stream);
            }

            return nodeSet;
        }

        private uint GenerateHashCode(UANodeSet nodeSet)
        {
            // generate a hash from the Model URIs and their version info in the nodeset
            int hashCode = 0;
            try
            {
                if ((nodeSet.Models != null) && (nodeSet.Models.Length > 0))
                {
                    foreach (ModelTableEntry model in nodeSet.Models)
                    {
                        if (model != null)
                        {
                            if (Uri.IsWellFormedUriString(model.ModelUri, UriKind.Absolute) && model.PublicationDateSpecified)
                            {
                                hashCode ^= model.ModelUri.GetDeterministicHashCode();
                                hashCode ^= model.PublicationDate.ToString(CultureInfo.InvariantCulture).GetDeterministicHashCode();
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    }
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }

        private uint GenerateHashCodeLegacy(UANodeSet nodeSet)
        {
            // generate a hash from the NamespaceURIs in the nodeset
            if (nodeSet?.NamespaceUris == null)
            {
                return 0;
            }
            int hashCode = 0;
            try
            {
                List<string> namespaces = new List<string>();
                foreach (string namespaceUri in nodeSet.NamespaceUris)
                {
                    if (!namespaces.Contains(namespaceUri))
                    {
                        namespaces.Add(namespaceUri);
                        hashCode ^= namespaceUri.GetDeterministicHashCode();
                    }
                }
            }
            catch (Exception)
            {
                return 0;
            }

            return (uint)hashCode;
        }

        public async Task<string> AddMetaDataAsync(UANameSpace uaNamespace, UANodeSet nodeSet, uint legacyNodesetHashCode, string userId)
        {
            string message = "Internal error: transaction not executed";
            try
            {
                // NodeSetModel deletion relies on database cascading delete. The deletions need to be saved for this to happen before we can re-add the same nodeset
                // To achieve consistency across delete and re-add, the operations need to run under a database transaction

                await _dbContext.Database.CreateExecutionStrategy().ExecuteInTransactionAsync(async () => {

                    message = null;

                    await DeleteAllRecordsForNodesetAsync(uaNamespace.Nodeset.Identifier).ConfigureAwait(false);

                    // delete any matching legacy nodesets
                    if (legacyNodesetHashCode != 0)
                    {
                        await DeleteAllRecordsForNodesetAsync(legacyNodesetHashCode).ConfigureAwait(false);
                    }

                    NodeSetModel existingModel = await _dbContext.NodeSetsWithUnapproved.FindAsync(nodeSet.Models[0].ModelUri, nodeSet.Models[0].GetNormalizedPublicationDate()).ConfigureAwait(false);
                    if (existingModel != null)
                    {
                        message = "Error: nodeset still exists after delete.";
                        return;
                    }

                    NamespaceMetaDataModel nameSpaceModel = MapRESTNamespaceToNamespaceMetaDataModel(uaNamespace, userId);


                    await _dbContext.AddAsync(nameSpaceModel).ConfigureAwait(false);

                    await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                },
                // This will only run on failures during transaction commit, where the EF can not determine if the Tx was committed or not
                () => _dbContext.NodeSetsWithUnapproved.AnyAsync(
                    n => n.ModelUri == nodeSet.Models[0].ModelUri && n.PublicationDate == (nodeSet.Models[0].PublicationDateSpecified ? nodeSet.Models[0].PublicationDate : default))
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                message = "Error: Could not save nodeset metadata";
            }

            return message;
        }

        public async Task IndexNodeSetModelAsync(UANodeSet nodeset, UANameSpace uaNamespace)
        {
            if (nodeset.Models.Length == 0)
            {
                throw new ArgumentException($"Invalid nodeset: no models specified");
            }

            NodeSetModel nodesetModel = MapRESTNodesetXmlToNodeSetModel(nodeset.Models[0], uaNamespace);

            // find our metadata model and link the two together
            NamespaceMetaDataModel metadataModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetModel.Identifier).ConfigureAwait(false);
            metadataModel.NodeSet = nodesetModel;
            nodesetModel.Metadata = metadataModel;

            new NodeModelFactoryOpc(nodesetModel, nodeset, _logger).ImportNodeSet();

            await _dbContext.NodeSetsWithUnapproved.AddAsync(nodesetModel).ConfigureAwait(false);

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<uint> IncrementDownloadCountAsync(uint nodesetId)
        {
            NamespaceMetaDataModel namespaceMeta = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);

            namespaceMeta.NumberOfDownloads++;
            uint newCount = namespaceMeta.NumberOfDownloads;

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            return newCount;
        }

        public async Task DeleteAllRecordsForNodesetAsync(uint nodesetId)
        {
            try
            {
                string nodesetIdStr = nodesetId.ToString(CultureInfo.InvariantCulture);
                NodeSetModel nodeSetModel = await _dbContext.NodeSetsWithUnapproved.FirstOrDefaultAsync(n => n.Identifier == nodesetIdStr).ConfigureAwait(false);
                if (nodeSetModel != null)
                {
                    _dbContext.NodeSetsWithUnapproved.Remove(nodeSetModel);
                }

                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved.FirstOrDefaultAsync(n => n.NodesetId == nodesetIdStr).ConfigureAwait(false);
                if (namespaceModel != null)
                {
                    _dbContext.NamespaceMetaDataWithUnapproved.Remove(namespaceModel);
                }

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while deleting all records for {nodesetId}");

                throw;
            }
        }

        public async Task<UANameSpace> RetrieveAllMetadataAsync(uint nodesetId, string userId)
        {
            try
            {
                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved
                    .Where(md => md.NodesetId == nodesetId.ToString(CultureInfo.InvariantCulture) && ((userId == "admin") || (md.UserId == userId) || string.IsNullOrEmpty(md.UserId)))
                    .Include(md => md.NodeSet)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                if (namespaceModel == null)
                {
                    return null;
                }

                return MapNamespaceMetaDataModelToRESTNamespace(namespaceModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }

        internal IQueryable<NodeSetModel> SearchNodesets(string[] keywords, string userId)
        {
            IQueryable<NodeSetModel> matchingNodeSets;

            if ((keywords != null) && (keywords.Length != 0) && (keywords[0] != "*"))
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";
                matchingNodeSets =
                    NodeSets
                    .Where(nsm =>
                        NamespaceMetaData.Any(md =>
                            (md.NodesetId == nsm.Identifier)
                            && ((userId == "admin") || (md.UserId == userId) || string.IsNullOrEmpty(md.UserId))
                            && (Regex.IsMatch(md.Title, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.Description, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(md.NodeSet.ModelUri, keywordRegex, RegexOptions.IgnoreCase)
                            || Regex.IsMatch(string.Join(",", md.Keywords), keywordRegex, RegexOptions.IgnoreCase)
                            )
                        )
                    );
            }
            else
            {
                matchingNodeSets = NodeSets.AsQueryable().Where(nsm => (userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId));
            }

            return matchingNodeSets;
        }

        public UANameSpace[] FindNodesets(string[] keywords, string userId, int? offset, int? limit)
        {
            var uaNamespaceModel = SearchNodesets(keywords, userId)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .Select(n => NamespaceMetaData.Where(nmd => nmd.NodesetId == n.Identifier && ((userId == "admin") || (nmd.UserId == userId) || string.IsNullOrEmpty(nmd.UserId)))
                .Include(nmd => nmd.NodeSet).FirstOrDefault())
                .ToList();

            return uaNamespaceModel.Select(MapNamespaceMetaDataModelToRESTNamespace).ToArray();
        }

        public Task<string[]> GetAllNamespacesAndNodesets(string userId)
        {
            try
            {
                string[] namesAndIds = NodeSets
                    .Where(nsm => (userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId))
                    .Select(nsm => new { nsm.ModelUri, nsm.Identifier, nsm.Version, nsm.PublicationDate })
                    .Select(n => $"{n.ModelUri},{n.Identifier},{n.Version},{n.PublicationDate}")
                    .ToArray();

                return Task.FromResult(namesAndIds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.FromResult(Array.Empty<string>());
        }

        public Task<string[]> GetAllNamesAndNodesets(string userId)
        {
            try
            {
                var categoryAndNodesetIds = NamespaceMetaData
                    .Where(md => (userId == "admin") || (md.UserId == userId) || string.IsNullOrEmpty(md.UserId))
                    .Select(md => new { md.Title, md.NodesetId })
                    .ToList();

                string[] namesAndNodesetsString = categoryAndNodesetIds
                    .Select(cn => $"{cn.Title},{cn.NodesetId}")
                    .ToArray();

                return Task.FromResult(namesAndNodesetsString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }

            return Task.FromResult(Array.Empty<string>());
        }

        public async Task<NamespaceMetaDataModel> ApproveNamespaceAsync(string identifier, ApprovalStatus status, string approvalInformation)
        {
            NamespaceMetaDataModel nodeSetMeta = await _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.NodesetId == identifier).FirstOrDefaultAsync().ConfigureAwait(false);
            if (nodeSetMeta == null) return null;

            nodeSetMeta.ApprovalStatus = status;
            nodeSetMeta.ApprovalInformation = approvalInformation;

            if (status == ApprovalStatus.Canceled)
            {
                try
                {
                    await _storage.DeleteFileAsync(nodeSetMeta.NodesetId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to delete file on Approval cancelation for {nodeSetMeta.NodesetId}: {ex.Message}");
                }

                await DeleteAllRecordsForNodesetAsync(uint.Parse(nodeSetMeta.NodesetId, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                return nodeSetMeta;
            }

            _dbContext.Attach(nodeSetMeta);

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            NamespaceMetaDataModel nodeSetMetaSaved = await _dbContext.NamespaceMetaDataWithUnapproved.Where(n => n.NodesetId == identifier).FirstOrDefaultAsync().ConfigureAwait(false);

            return nodeSetMetaSaved;
        }

        private NamespaceMetaDataModel MapRESTNamespaceToNamespaceMetaDataModel(UANameSpace uaNamespace, string userId)
        {
            // fix up license expression
            string licenseExpression = uaNamespace.License switch {
                "0" => "MIT",
                "1" or "ApacheLicense20" => "Apache-2.0",
                "2" => "Custom",
                _ => uaNamespace.License
            };

            NamespaceMetaDataModel metadataModel = new() {
                NodesetId = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture),
                Title = uaNamespace.Title,
                License = licenseExpression,
                CopyrightText = uaNamespace.CopyrightText,
                Description = uaNamespace.Description,
                DocumentationUrl = uaNamespace.DocumentationUrl?.ToString(),
                IconUrl = uaNamespace.IconUrl?.ToString(),
                LicenseUrl = uaNamespace.LicenseUrl?.ToString(),
                Keywords = uaNamespace.Keywords,
                PurchasingInformationUrl = uaNamespace.PurchasingInformationUrl?.ToString(),
                ReleaseNotesUrl = uaNamespace.ReleaseNotesUrl?.ToString(),
                TestSpecificationUrl = uaNamespace.TestSpecificationUrl?.ToString(),
                SupportedLocales = uaNamespace.SupportedLocales,
                NumberOfDownloads = uaNamespace.NumberOfDownloads,
                ApprovalStatus = ApprovalStatus.Pending,
                CreationTime = uaNamespace.CreationTime != null ? uaNamespace.CreationTime.GetNormalizedDate() : DateTime.UtcNow,
                UserId = userId
            };

            return metadataModel;
        }

        private NodeSetModel MapRESTNodesetXmlToNodeSetModel(ModelTableEntry nodesetTableEntry, UANameSpace uaNamespace)
        {
            NodeSetModel nodesetModel = new() {
                ModelUri = nodesetTableEntry.ModelUri,
                Version = nodesetTableEntry.Version,
                PublicationDate = nodesetTableEntry.GetNormalizedPublicationDate(),
                Identifier = uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture),
                LastModifiedDate = NodeModelUtils.GetNormalizedDate(uaNamespace.Nodeset.LastModifiedDate)
            };

            if (nodesetTableEntry.RequiredModel != null)
            {
                foreach (ModelTableEntry requiredModel in nodesetTableEntry.RequiredModel)
                {
                    var requiredModelInfo = new RequiredModelInfoModel {
                        ModelUri = requiredModel.ModelUri,
                        PublicationDate = requiredModel.PublicationDateSpecified ? ((DateTime?)requiredModel.PublicationDate).GetNormalizedDate() : null,
                        Version = requiredModel.Version,
                        AvailableModel = null // will be filled in when the nodeset is queried via REST API
                    };

                    nodesetModel.RequiredModels.Add(requiredModelInfo);
                }
            }

            return nodesetModel;
        }

        private UANameSpace MapNamespaceMetaDataModelToRESTNamespace(NamespaceMetaDataModel metadataModel)
        {
            return new UANameSpace {
                CreationTime = metadataModel.CreationTime,
                Title = metadataModel.Title,
                License = metadataModel.License,
                CopyrightText = metadataModel.CopyrightText,
                Description = metadataModel.Description,
                DocumentationUrl = metadataModel.DocumentationUrl != null ? new Uri(metadataModel.DocumentationUrl) : null,
                IconUrl = metadataModel.IconUrl != null ? new Uri(metadataModel.IconUrl) : null,
                LicenseUrl = metadataModel.LicenseUrl != null ? new Uri(metadataModel.LicenseUrl) : null,
                Keywords = metadataModel.Keywords,
                PurchasingInformationUrl = metadataModel.PurchasingInformationUrl != null ? new Uri(metadataModel.PurchasingInformationUrl) : null,
                ReleaseNotesUrl = metadataModel.ReleaseNotesUrl != null ? new Uri(metadataModel.ReleaseNotesUrl) : null,
                TestSpecificationUrl = metadataModel.TestSpecificationUrl != null ? new Uri(metadataModel.TestSpecificationUrl) : null,
                SupportedLocales = metadataModel.SupportedLocales,
                NumberOfDownloads = metadataModel.NumberOfDownloads,
                Nodeset = metadataModel.NodeSet == null ? null : MapNodeSetModelToRESTNodeSet(metadataModel.NodeSet)
            };
        }

        private Nodeset MapNodeSetModelToRESTNodeSet(NodeSetModel model)
        {
            return new Nodeset {
                Identifier = uint.Parse(model.Identifier, CultureInfo.InvariantCulture),
                NamespaceUri = model.ModelUri != null ? new Uri(model.ModelUri) : null,
                PublicationDate = model.PublicationDate ?? default,
                LastModifiedDate = model.LastModifiedDate ?? default,
                Version = model.Version,
                NodesetXml = null,
                RequiredModels = model.RequiredModels.Select(rm => {

                    List<NodeSetModel> matchingNodesets = _dbContext.Set<NodeSetModel>().Where(nsm => nsm.ModelUri == rm.ModelUri).ToList();
                    NodeSetModel availableNodeset = NodeModelUtils.GetMatchingOrHigherNodeSet(matchingNodesets, rm.PublicationDate, rm.Version);

                    return new RequiredModelInfo {
                        NamespaceUri = rm.ModelUri,
                        PublicationDate = rm.PublicationDate,
                        Version = rm.Version,
                        AvailableModel = (availableNodeset != null) ? MapNodeSetModelToRESTNodeSet(availableNodeset) : null
                    };
                }).ToList()
            };
        }

        public async Task<string[]> GetAllTypes(string userId, string nodeSetID)
        {
            NodeSetModel nodeSetMeta = await GetNodeSets(userId, nodeSetID).FirstOrDefaultAsync().ConfigureAwait(false);
            if (nodeSetMeta != null)
            {
                List<NodeModel> types =
                [
                    .. GetNodeModels(nsm => nsm.ObjectTypes, userId, nodeSetMeta.ModelUri),
                    .. GetNodeModels(nsm => nsm.VariableTypes, userId, nodeSetMeta.ModelUri),
                    .. GetNodeModels(nsm => nsm.DataTypes, userId, nodeSetMeta.ModelUri),
                    .. GetNodeModels(nsm => nsm.ReferenceTypes, userId, nodeSetMeta.ModelUri),
                ];

                List<string> typeList = new();
                foreach (NodeModel model in types)
                {
                    string expandedNodeIdWithBrowseName = model.NodeId + ";" + model.BrowseName;
                    typeList.Add(expandedNodeIdWithBrowseName);
                }

                return typeList.ToArray();
            }

            return Array.Empty<string>();
        }

        public async Task<string[]> GetAllInstances(string userId, string nodeSetID)
        {
            NodeSetModel nodeSetMeta = await GetNodeSets(userId, nodeSetID).FirstOrDefaultAsync().ConfigureAwait(false);
            if (nodeSetMeta != null)
            {
                List<NodeModel> instances =
                [
                    .. GetNodeModels(nsm => nsm.Objects, userId, nodeSetMeta.ModelUri),
                    .. GetNodeModels(nsm => nsm.Properties, userId, nodeSetMeta.ModelUri),
                    .. GetNodeModels(nsm => nsm.DataVariables, userId, nodeSetMeta.ModelUri),
                ];

                List<string> instanceList = new();
                foreach (NodeModel model in instances)
                {
                    string expandedNodeIdWithBrowseName = model.NodeId + ";" + model.BrowseName;
                    instanceList.Add(expandedNodeIdWithBrowseName);
                }

                return instanceList.ToArray();
            }

            return Array.Empty<string>();
        }

        public string GetNode(string expandedNodeId, string identifier, string userId)
        {
            // create a substring from expandedNodeId by removing "nsu=" from the start and parsing until the first ";"
            string modelUri = expandedNodeId.Substring(4, expandedNodeId.IndexOf(';', StringComparison.OrdinalIgnoreCase) - 4);

            NodeSetModel matchingNodeset = _dbContext.Set<NodeSetModel>().Where(nsm => nsm.Identifier == identifier).FirstOrDefault();

            ObjectTypeModel objectTypeModel = GetNodeModels(nsm => nsm.ObjectTypes, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (objectTypeModel != null)
            {
                return "Object Type;" + objectTypeModel.ToString();
            }

            VariableTypeModel variableTypeModel = GetNodeModels(nsm => nsm.VariableTypes, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (variableTypeModel != null)
            {
                return "Variable Type;" + variableTypeModel.ToString();
            }

            DataTypeModel dataTypeModel = GetNodeModels(nsm => nsm.DataTypes, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (dataTypeModel != null)
            {
                return "Data Type;" + dataTypeModel.ToString();
            }

            ReferenceTypeModel referenceTypeModel = GetNodeModels(nsm => nsm.ReferenceTypes, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (referenceTypeModel != null)
            {
                return "Reference Type;" + referenceTypeModel.ToString();
            }

            ObjectModel objectModel = GetNodeModels(nsm => nsm.Objects, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (objectModel != null)
            {
                return "Object;" + objectModel.ToString();
            }

            PropertyModel propertyModel = GetNodeModels(nsm => nsm.Properties, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (propertyModel != null)
            {
                return "Property;" + propertyModel.ToString();
            }

            DataVariableModel variableModel = GetNodeModels(nsm => nsm.DataVariables, modelUri, userId, matchingNodeset.PublicationDate, expandedNodeId).FirstOrDefault();
            if (variableModel != null)
            {
                return "Variable;" + variableModel.ToString();
            }

            return string.Empty;
        }
    }
}
