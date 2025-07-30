/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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
using System.Linq;
using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Make the UANodeSetImporter work over IFileStorage
    /// </summary>
    public class NodeSetImporter
    {
        public NodeSetImporter(DbFileStorage storage, AppDbContext dbContext)
        {
            _storage = storage;
            _dbContext = dbContext;
        }

        private readonly DbFileStorage _storage;
        private readonly AppDbContext _dbContext;

        public bool ImportNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID, bool requested)
        {
            // Assume already added to cloudlib storage before
            Export.UANodeSet nodeSet = InfoModelController.ReadUANodeSet(nodeSetXml);

            // Assumption: exactly one nodeSetXml was passed into UANodeSetImport.ImportNodeSets and it's the first one being added.
            bool isNew = (results.Models.Count == 0);
            (ModelValue Model, bool Added) modelInfo = ImportModelAndDependencies(results, nodeSet, null, nodeSet.Models?[0], null, isNew);
            modelInfo.Model.RequestedForThisImport = requested;

            return modelInfo.Added;
        }

        /// <summary>
        /// Parses Dependencies and creates the Models- and MissingModels collection
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodeSet"></param>
        /// <param name="ns"></param>
        /// <param name="filePath"></param>
        /// <param name="WasNewSet"></param>
        /// <returns>The ModelValue created or found in the results</returns>
        public (ModelValue Model, bool Added) ImportModelAndDependencies(UANodeSetImportResult results, UANodeSet nodeSet, string headerComment, ModelTableEntry ns, string filePath, bool wasNewFile)
        {
            NodeModelUtils.FixupNodesetVersionFromMetadata(nodeSet, null);
            bool bAdded = false;
            var tModel = GetMatchingOrHigherModel(results, ns.ModelUri, ns.GetNormalizedPublicationDate(), ns.Version);
            if (tModel == null)
            {
                // Remove any previous models with this ModelUri, as we have found a newer one
                if (results.Models.RemoveAll(m => m.NameVersion.ModelUri == ns.ModelUri) > 0)
                {
                    // superceded
                }

                tModel = new ModelValue { NodeSet = nodeSet, HeaderComment = headerComment, NameVersion = new ModelNameAndVersion(ns), FilePath = filePath, NewInThisImport = wasNewFile };
                results.Models.Add(tModel);
                bAdded = true;
            }

            if (ns.RequiredModel?.Length > 0)
            {
                foreach (var tDep in ns.RequiredModel)
                {
                    tModel.Dependencies.Add(tDep.ModelUri);
                    if (!results.MissingModels.Any(s => s.HasNameAndVersion(tDep.ModelUri, tDep.GetNormalizedPublicationDate(), tDep.Version)))
                    {
                        results.MissingModels.Add(new ModelNameAndVersion(tDep));
                    }
                }
            }

            return (tModel, bAdded);
        }

        private ModelValue GetMatchingOrHigherModel(UANodeSetImportResult results, string modelUri, DateTime? publicationDate, string version)
        {
            var matchingNodeSetsForUri = results.Models
                .Where(s => s.NameVersion.ModelUri == modelUri)
                .Select(m =>
                    new NodeSetModel {
                        ModelUri = m.NameVersion.ModelUri,
                        PublicationDate = m.NameVersion.PublicationDate,
                        Version = m.NameVersion.ModelVersion,
                        CustomState = m,
                    });

            var matchingNodeSet = NodeSetVersionUtils.GetMatchingOrHigherNodeSet(matchingNodeSetsForUri, publicationDate, version);
            var tModel = matchingNodeSet?.CustomState as ModelValue;

            if (tModel == null && matchingNodeSet != null)
            {
                throw new InvalidCastException("Internal error: CustomState not preserved");
            }

            return tModel;
        }

        /// <summary>
        /// Updates missing dependencies of NodesSets based on all loaded nodsets
        /// </summary>
        /// <param name="results"></param>
        public void ResolveDependencies(UANodeSetImportResult results)
        {
            if (results?.Models?.Count > 0 && results?.MissingModels?.Count > 0)
            {
                for (int i = results.MissingModels.Count - 1; i >= 0; i--)
                {
                    if (results.Models.Any(s => s.NameVersion.IsNewerOrSame(results.MissingModels[i])))
                    {
                        results.MissingModels.RemoveAt(i);
                    }
                }
            }
        }

        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID)
        {
            // Find next higher model if no exact match
            var matchingNodeSet = DbOpcUaContext.GetMatchingOrHigherNodeSetAsync(_dbContext, nameVersion.ModelUri, nameVersion.PublicationDate, nameVersion.ModelVersion).Result as CloudLibNodeSetModel;
            if (matchingNodeSet != null)
            {
                string tFileName = matchingNodeSet.Identifier;
                string nodeSetXml = _storage.DownloadFileAsync(tFileName).Result;
                if (nodeSetXml != null)
                {
                    ImportNodeSet(results, nodeSetXml, TenantID, false);
                    return true;
                }
            }

            return false;
        }
    }
}
