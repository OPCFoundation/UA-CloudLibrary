/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 *
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.Models
{
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
    }
}
