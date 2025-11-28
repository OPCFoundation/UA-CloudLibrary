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
using LinqKit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibDataProvider
    {
        private readonly AppDbContext _dbContext = null;
        private readonly DbFileStorage _storage;
        private readonly ILogger _logger;

        public CloudLibDataProvider(AppDbContext context, ILoggerFactory logger, DbFileStorage storage, IConfiguration configuration)
        {
            _dbContext = context;
            _storage = storage;
            _logger = logger.CreateLogger("CloudLibDataProvider");
        }

        private Expression<Func<NamespaceMetaDataModel, bool>> GetMetadataUserFilter(string userId)
        {
            return nsm =>
                (userId == "admin") ||
                (nsm.UserId == "admin") ||
                (nsm.UserId == userId) ||
                string.IsNullOrEmpty(nsm.UserId);
        }

        private Expression<Func<NodeSetModel, bool>> GetNodesetUserFilter(string userId)
        {
            return nsm =>
                (userId == "admin") ||
                (nsm.Metadata.UserId == "admin") ||
                (nsm.Metadata.UserId == userId) ||
                string.IsNullOrEmpty(nsm.Metadata.UserId);
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

                nodeSets = _dbContext.NodeSetsWithUnapproved
                    .AsExpandable()
                    .Where(nsm => nsm.Identifier == identifier)
                    .Where(GetNodesetUserFilter(userId));
            }
            else if (!string.IsNullOrEmpty(modelUri) && (publicationDate == null) && (keywords == null))
            {
                nodeSets = _dbContext.NodeSetsWithUnapproved
                    .AsExpandable()
                    .Where(nsm => nsm.ModelUri == modelUri)
                    .Where(GetNodesetUserFilter(userId));
            }
            else
            {
                IQueryable<NodeSetModel> nodeSetQuery = SearchNodesets(userId, keywords);
                if (!string.IsNullOrEmpty(modelUri) && (publicationDate != null))
                {
                    nodeSets = nodeSetQuery
                        .AsExpandable()
                        .Where(nsm => nsm.ModelUri == modelUri)
                        .Where(nsm => nsm.PublicationDate == publicationDate)
                        .Where(GetNodesetUserFilter(userId));
                }
                else if (!string.IsNullOrEmpty(modelUri))
                {
                    nodeSets = nodeSetQuery
                        .AsExpandable()
                        .Where(nsm => nsm.ModelUri == modelUri)
                        .Where(GetNodesetUserFilter(userId));
                }
                else
                {
                    nodeSets = nodeSetQuery
                        .Where(GetNodesetUserFilter(userId));
                }
            }

            return nodeSets;
        }

        public IQueryable<T> GetNodeModels<T>(Expression<Func<NodeSetModel, IEnumerable<T>>> selector, string userId, string modelUri = null, DateTime? publicationDate = null, string nodeId = null)
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
                nodeSets = _dbContext.NodeSetsWithUnapproved
                    .AsExpandable()
                    .Where(nsm => nsm.ModelUri == modelUri)
                    .Where(nsm => nsm.PublicationDate == publicationDate)
                    .Where(GetNodesetUserFilter(userId));
            }
            else if (modelUri != null)
            {
                nodeSets = _dbContext.NodeSetsWithUnapproved
                    .AsExpandable()
                    .Where(nsm => nsm.ModelUri == modelUri)
                    .Where(GetNodesetUserFilter(userId));
            }
            else
            {
                nodeSets = _dbContext.NodeSetsWithUnapproved
                    .Where(GetNodesetUserFilter(userId));
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

        public async Task<string> UploadNamespaceAndNodesetAsync(string userId, UANameSpace uaNamespace, string values, bool overwrite)
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
                            .Where(n => (n.NodesetId == legacyNodesetHashCode.ToString(CultureInfo.InvariantCulture)) && ((userId == "admin") || (n.UserId == "admin") || (n.UserId == userId) || string.IsNullOrEmpty(n.UserId)))
                            .Include(n => n.NodeSet)
                            .FirstOrDefault();

                        if (existingLegacyNamespaces != null)
                        {
                            // we treat no user in the database like an admin user
                            if ((string.IsNullOrEmpty(existingLegacyNamespaces.UserId) || (existingLegacyNamespaces.UserId == "admin")) && (userId != "admin"))
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
                UANameSpace existingNamespace = await RetrieveAllMetadataAsync(userId, uaNamespace.Nodeset.Identifier).ConfigureAwait(false);
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
                .Where(n => (n.NodesetId == uaNamespace.Nodeset.Identifier.ToString(CultureInfo.InvariantCulture)) && ((userId == "admin") || (n.UserId == "admin") || (n.UserId == userId) || string.IsNullOrEmpty(n.UserId)))
                .Include(n => n.NodeSet)
                .FirstOrDefault();

            if (existingNamespaces != null)
            {
                // we treat no user in the database like an admin user
                if ((string.IsNullOrEmpty(existingNamespaces.UserId) || (existingNamespaces.UserId == "admin")) && (userId != "admin"))
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

            string dbMessage = await AddMetaDataAsync(userId, uaNamespace, nodeSet, legacyNodesetHashCode).ConfigureAwait(false);
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

        public async Task<string> AddMetaDataAsync(string userId, UANameSpace uaNamespace, UANodeSet nodeSet, uint legacyNodesetHashCode)
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

                    NamespaceMetaDataModel nameSpaceModel = MapRESTNamespaceToNamespaceMetaDataModel(userId, uaNamespace);


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

        public async Task<UANameSpace> RetrieveAllMetadataAsync(string userId, uint nodesetId)
        {
            try
            {
                NamespaceMetaDataModel namespaceModel = await _dbContext.NamespaceMetaDataWithUnapproved
                    .AsExpandable()
                    .Where(md => md.NodesetId == nodesetId.ToString(CultureInfo.InvariantCulture))
                    .Where(GetMetadataUserFilter(userId))
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

        internal IQueryable<NodeSetModel> SearchNodesets(string userId, string[] keywords)
        {
            IQueryable<NodeSetModel> matchingNodeSets;

            if ((keywords != null) && (keywords.Length != 0) && (keywords[0] != "*"))
            {
                string keywordRegex = $".*({string.Join('|', keywords)}).*";

                matchingNodeSets = _dbContext.NodeSetsWithUnapproved
                    .AsExpandable()
                    .Where(GetNodesetUserFilter(userId))
                    .Where(md => (Regex.IsMatch(md.Metadata.Title, keywordRegex, RegexOptions.IgnoreCase)
                        || Regex.IsMatch(md.Metadata.Description, keywordRegex, RegexOptions.IgnoreCase)
                        || Regex.IsMatch(md.Metadata.NodeSet.ModelUri, keywordRegex, RegexOptions.IgnoreCase)
                        || Regex.IsMatch(string.Join(",", md.Metadata.Keywords), keywordRegex, RegexOptions.IgnoreCase)
                    )
                );
            }
            else
            {
                matchingNodeSets = _dbContext.NodeSetsWithUnapproved
                    .Where(GetNodesetUserFilter(userId));
            }

            return matchingNodeSets;
        }

        public UANameSpace[] FindNodesets(string userId, string[] keywords, string namespaceUri, int? offset, int? limit)
        {
            List<NamespaceMetaDataModel> uaNamespaceModels = SearchNodesets(userId, keywords)
                .OrderBy(n => n.ModelUri)
                .Skip(offset ?? 0)
                .Take(limit ?? 100)
                .Select(n => _dbContext.NamespaceMetaDataWithUnapproved
                    .AsExpandable()
                    .Where(nmd => (namespaceUri == null) || (nmd.NodeSet.ModelUri == namespaceUri))
                    .Where(nmd => nmd.NodesetId == n.Identifier)
                    .Where(GetMetadataUserFilter(userId))
                .Include(nmd => nmd.NodeSet).FirstOrDefault())
                .Where(n => n != null)
                .ToList();

            return uaNamespaceModels.Select(MapNamespaceMetaDataModelToRESTNamespace).ToArray();
        }

        public Task<string[]> GetAllNamespacesAndNodesets(string userId)
        {
            try
            {
                string[] namesAndIds = _dbContext.NodeSetsWithUnapproved
                    .Where(GetNodesetUserFilter(userId))
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
                var categoryAndNodesetIds = _dbContext.NamespaceMetaDataWithUnapproved
                    .Where(GetMetadataUserFilter(userId))
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

        private NamespaceMetaDataModel MapRESTNamespaceToNamespaceMetaDataModel(string userId, UANameSpace uaNamespace)
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
                ApprovalStatus = ApprovalStatus.Approved,
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
            if (metadataModel == null)
            {
                return null;
            }

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
                    typeList.Add(model.NodeId + "," + model.BrowseName);
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
                    instanceList.Add(model.NodeId + "," + model.BrowseName);
                }

                return instanceList.ToArray();
            }

            return Array.Empty<string>();
        }

        public void UpdateAssetAdministrationShellDescriptor(string userName, string decodedAasIdentifier, AssetAdministrationShellDescriptor shellDescriptor)
        {
            // check user permissions
            if (!GetNodeSets(userName, decodedAasIdentifier).Any())
            {
                throw new UnauthorizedAccessException("User is not authorized to update this AAS.");
            }

            foreach (var assetId in shellDescriptor.SpecificAssetIds)
            {
                PersistedSpecificAssetIds newEntry = new() {
                    AASId = decodedAasIdentifier,
                    AssetId = assetId
                };

                // check if this one is already added
                if (_dbContext.PersistedSpecificAssetIds.Any(e => e.AASId == decodedAasIdentifier && e.AssetId.Name == assetId.Name && e.AssetId.Value == assetId.Value))
                {
                    continue;
                }
                else
                {
                    _dbContext.PersistedSpecificAssetIds.Add(newEntry);
                }
            }

            _dbContext.SaveChanges();
        }

        internal List<SpecificAssetId> GetAssetAdministrationShellDescriptor(string userName, string decodedAasIdentifier)
        {
            // check user permissions
            if (!GetNodeSets(userName, decodedAasIdentifier).Any())
            {
                throw new UnauthorizedAccessException("User is not authorized to update this AAS.");
            }

            return _dbContext.PersistedSpecificAssetIds
                .Where(e => e.AASId == decodedAasIdentifier)
                .Select(e => e.AssetId)
                .ToList();
        }
    }
}
