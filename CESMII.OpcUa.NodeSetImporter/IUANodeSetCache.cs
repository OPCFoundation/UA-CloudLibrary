/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.Extensions.Logging;
using Opc.Ua.Export;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CESMII.OpcUa.NodeSetImporter
{
    public interface IUANodeSetCache
    {
        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID);
        public bool AddNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID, bool requested);
        public string GetRawModelXML(ModelValue model);
        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results);
        public UANodeSetImportResult FlushCache();
        public ModelValue GetNodeSetByID(string id);
    }

    /// <summary>
    /// Model Value containing all important fast access datapoints of a model
    /// </summary>
    public class ModelValue
    {
        /// <summary>
        /// The imported NodeSet - use this in your subsequent code
        /// </summary>
        public UANodeSet NodeSet { get; set; }
        public string HeaderComment { get; set; }
        /// <summary>
        /// File Path to the XML file cache of the NodeSet on the Server
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// List of all Model URI (Namespace) dependencies of the Model
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();
        /// <summary>
        /// Name and Version of NodeSet
        /// </summary>
        public ModelNameAndVersion NameVersion { get; set; }
        /// <summary>
        /// a Flag telling the consumer that this model was just found and new to this import
        /// </summary>
        public bool NewInThisImport { get; set; }

        /// <summary>
        /// A flag telling the consumer that this model is one of the explicitly requested nodemodel, even if it already existed
        /// </summary>
        public bool RequestedForThisImport { get; set; }

        public override string ToString()
        {
            return $"{NameVersion}";
        }
    }

    /// <summary>
    /// Result-Set of this Importer
    /// Check "ErrorMessage" for issues during the import such as missing dependencies
    /// Check "MissingModels" as a list of Models that could not be resolved 
    /// </summary>
    public class UANodeSetImportResult
    {
        /// <summary>
        /// Error Message in case the import was not successful or is missing dependencies
        /// </summary>
        public string ErrorMessage { get; set; } = "";
        /// <summary>
        /// All Imported Models - sorted from least amount of dependencies to most dependencies
        /// </summary>
        public List<ModelValue> Models { get; set; } = new List<ModelValue>();
        /// <summary>
        /// List if missing models listed as ModelUri strings
        /// </summary>
        public List<ModelNameAndVersion> MissingModels { get; set; } = new List<ModelNameAndVersion>();
        /// <summary>
        /// A NodeSet author might add custom "Extensions" to a NodeSet. 
        /// </summary>
        public Dictionary<string, string> Extensions { get; set; } = new Dictionary<string, string>();


        /// <summary>
        /// Parses Dependencies and creates the Models- and MissingModels collection
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodeSet"></param>
        /// <param name="ns"></param>
        /// <param name="filePath"></param>
        /// <param name="WasNewSet"></param>
        /// <returns>The ModelValue created or found in the results</returns>
        public (ModelValue Model, bool Added) AddModelAndDependencies(UANodeSet nodeSet, string headerComment, ModelTableEntry ns, string filePath, bool wasNewFile, ILogger logger = null)
        {
            NodeModelUtils.FixupNodesetVersionFromMetadata(nodeSet, logger);
            bool bAdded = false;
            var tModel = GetMatchingOrHigherModel(ns.ModelUri, ns.GetNormalizedPublicationDate(), ns.Version);
            if (tModel == null)
            {
                // Remove any previous models with this ModelUri, as we have found a newer one
                if (this.Models.RemoveAll(m => m.NameVersion.ModelUri == ns.ModelUri) > 0)
                {
                    // superceded
                }

                tModel = new ModelValue { NodeSet = nodeSet, HeaderComment = headerComment, NameVersion = new ModelNameAndVersion(ns), FilePath = filePath, NewInThisImport = wasNewFile };
                this.Models.Add(tModel);
                bAdded = true;
            }
            if (ns.RequiredModel?.Any() == true)
            {
                foreach (var tDep in ns.RequiredModel)
                {
                    tModel.Dependencies.Add(tDep.ModelUri);
                    if (!this.MissingModels.Any(s => s.HasNameAndVersion(tDep.ModelUri, tDep.GetNormalizedPublicationDate(), tDep.Version)))
                    {
                        this.MissingModels.Add(new ModelNameAndVersion(tDep));
                    }
                }
            }
            return (tModel, bAdded);
        }

        private ModelValue GetMatchingOrHigherModel(string modelUri, DateTime? publicationDate, string version)
        {
            var matchingNodeSetsForUri = this.Models
                .Where(s => s.NameVersion.ModelUri == modelUri)
                .Select(m =>
                    new NodeSetModel.NodeSetModel
                    {
                        ModelUri = m.NameVersion.ModelUri,
                        PublicationDate = m.NameVersion.PublicationDate,
                        Version = m.NameVersion.ModelVersion,
                        CustomState = m,
                    });
            var matchingNodeSet = NodeSetModel.NodeSetVersionUtils.GetMatchingOrHigherNodeSet(matchingNodeSetsForUri, publicationDate, version);
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
        public void ResolveDependencies()
        {
            if (this?.Models?.Count > 0 && this?.MissingModels?.Count > 0)
            {
                for (int i = this.MissingModels.Count - 1; i >= 0; i--)
                {
                    if (this.Models.Any(s => s.NameVersion.IsNewerOrSame(this.MissingModels[i])))
                    {
                        this.MissingModels.RemoveAt(i);
                    }
                }
            }
        }

    }

}
