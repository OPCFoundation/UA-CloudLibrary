/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using Opc.Ua.Export;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CESMII.OpcUa.NodeSetImporter
{
    public interface IUANodeSetCache
    {
        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID);
        public bool AddNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID);
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
        /// TODO: Make a decision how we handle Comments. xml Comments (xcomment tags) are preserved but inline file comments // or /* */ are not preserved
        /// TODO: Do we need to use this dictionary at all? The Extensions are in The "Models/NodeSet/Extensions" anyway. 
        ///       We could use this to add our own (Profile Editor) extensions that the UA Exporter would write back to the Models/NodeSet/Extension fields
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
        public ModelValue AddModelAndDependencies(UANodeSet nodeSet, ModelTableEntry ns, string filePath, bool WasNewSet)
        {
            var tModel = this.Models.Where(s => s.NameVersion.ModelUri == ns.ModelUri).OrderByDescending(s => s.NameVersion.PublicationDate).FirstOrDefault();
            if (tModel == null)
            {
                this.Models.Add(tModel = new ModelValue { NodeSet = nodeSet, NameVersion = new ModelNameAndVersion { ModelUri = ns.ModelUri, ModelVersion = ns.Version, PublicationDate = ns.PublicationDate }, FilePath = filePath, NewInThisImport = WasNewSet });
            }
            if (ns.RequiredModel?.Any() == true)
            {
                foreach (var tDep in ns.RequiredModel)
                {
                    tModel.Dependencies.Add(tDep.ModelUri);
                    if (!this.MissingModels.Any(s => s.HasNameAndVersion(tDep.ModelUri, tDep.PublicationDate)))
                    {
                        this.MissingModels.Add(new ModelNameAndVersion { ModelUri = tDep.ModelUri, ModelVersion = tDep.Version, PublicationDate = tDep.PublicationDate });
                    }
                }
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
