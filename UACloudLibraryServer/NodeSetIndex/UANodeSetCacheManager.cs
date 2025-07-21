/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 *
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    //Glossary of Terms:
    //-----------------------------------
    //NodeSet - Container File of one or more Models
    //Model - a unique OPC UA Model identified with a unique NamespaceUri/ModelUri. A model can be spanned across multiple NodeSet (files)
    //Namespace - the unique identifier of a Model (also called ModelUri)
    //UAStandardModel - A Model that has been standardized by the OPC UA Foundation and can be found in the official schema store: https://files.opcfoundation.org/schemas/
    //UANodeSetImporter - Imports one or more OPC UA NodeSets resulting in a "NodeSetImportResult" containing all found Models and a list of missing dependencies

    /// <summary>
    /// Main Importer class importing NodeSets
    /// </summary>
    ///
    public interface IUANodeSetResolver
    {
        Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels);
    }

    public class UANodeSetCacheManager
    {
        UANodeSetImportResult _results = new();
        private readonly UANodeSetIFileStorage _nodeSetCacheSystem;
        private readonly IUANodeSetResolver _nodeSetResolver;

        public UANodeSetCacheManager(UANodeSetIFileStorage nodeSetCacheSystem)
        {
            _nodeSetCacheSystem = nodeSetCacheSystem;
            _nodeSetResolver = null;
        }

        /// <summary>
        /// Imports NodeSets from Files resolving dependencies using already uploaded NodeSets
        /// </summary>
        /// <param name="NodeSetCacheSystem">This interface can be used to override the default file cache of the Importer, i.e with a Database cache</param>
        /// <param name="previousResults">If null, a new resultset will be created. If not null already uploaded NodeSets can be augmented with New NodeSets referred in the FileNames</param>
        /// <param name="nodeSetFilenames">List of full paths to uploaded NodeSets</param>
        /// <param name="nodeSetStreams">List of streams containing NodeSets</param>
        /// <param name="FailOnExisting">Default behavior is that all Models in NodeSets are returned even if they have been imported before. If set to true, the importer will fail if it has imported a nodeset before and does not cache nodeset if they have missing dependencies</param>
        /// <param name="TenantID">If the import has Multi-Tenant Cache, the tenant ID has to be set here</param>
        /// <returns></returns>
        public UANodeSetImportResult ImportNodeSets(IEnumerable<string> nodeSetsXml, bool FailOnExisting = false, object TenantID = null)
        {
            _results.ErrorMessage = "";
            var previousMissingModels = new List<ModelNameAndVersion>();
            try
            {
                bool rerun;
                do
                {
                    rerun = false;
                    bool NewNodeSetFound = false;
                    if (nodeSetsXml != null)
                    {
                        // Must enumerate the nodeSetsXml only once in case the caller creates/loads strings as needed (streams of files)
                        foreach (var nodeSetXml in nodeSetsXml)
                        {
                            var JustFoundNewNodeSet = _nodeSetCacheSystem.AddNodeSet(_results, nodeSetXml, TenantID, true);
                            NewNodeSetFound |= JustFoundNewNodeSet;
                        }
                        nodeSetsXml = null;
                    }

                    if (!NewNodeSetFound && FailOnExisting)
                    {
                        string names = string.Join(", ", _results.Models.Select(m => m.NameVersion));
                        _results.ErrorMessage = $"All selected NodeSets or newer versions of them ({names}) have already been imported";
                        return _results;
                    }
                    if (_results.Models.Count == 0)
                    {
                        _results.ErrorMessage = "No Nodesets specified in either nodeSetFilenames or nodeSetStreams";
                        return _results;
                    }

                    _nodeSetCacheSystem.ResolveDependencies(_results);

                    if (_results?.MissingModels?.Any() == true)
                    {
                        foreach (var t in _results.MissingModels.ToList())
                        {
                            rerun |= _nodeSetCacheSystem.GetNodeSet(_results, t, TenantID);
                        }

                        _nodeSetCacheSystem.ResolveDependencies(_results);

                        if (_results.MissingModels.Any())
                        {
                            if (_results.MissingModels.SequenceEqual(previousMissingModels))
                            {
                                rerun = false;
                                continue;
                            }
                            previousMissingModels = _results.MissingModels.ToList();
                            // No more cached models were added, but we are still missing models: invoke the resolver if provided
                            if (_nodeSetResolver != null)
                            {
                                try
                                {
                                    var newNodeSetsXml = _nodeSetResolver.ResolveNodeSetsAsync(_results.MissingModels.ToList()).Result;
                                    if (newNodeSetsXml?.Any() == true)
                                    {
                                        nodeSetsXml = newNodeSetsXml;
                                        rerun = true;
                                        continue;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (_results.ErrorMessage.Length > 0) _results.ErrorMessage += ", ";
                                    _results.ErrorMessage += $"Error resolving missing nodesets: {ex.Message}";
                                }
                            }
                            if (_results.ErrorMessage.Length > 0) _results.ErrorMessage += ", ";
                            _results.ErrorMessage += string.Join(",", _results.MissingModels);
                        }
                        if (!rerun && !string.IsNullOrEmpty(_results.ErrorMessage))
                        {
                            _results.ErrorMessage = $"The following NodeSets are required: " + _results.ErrorMessage;
                        }
                    }

                    _results.Models = OrderByDependencies(_results.Models); // _results.Models.OrderBy(s => s.Dependencies.Count).ToList();
                } while (rerun && _results.MissingModels.Any());
                if (!_results.MissingModels.Any())
                {
                    _results.ErrorMessage = null;
                }
            }
            catch (Exception ex)
            {
                _results.ErrorMessage = ex.Message;
            }

            return _results;
        }

        static List<ModelValue> OrderByDependencies(List<ModelValue> models)
        {
            var remainingModels = new List<ModelValue>(models);
            var orderedModels = new List<ModelValue>();

            bool modelAdded;
            do
            {
                modelAdded = false;
                for (int i = 0; i < remainingModels.Count;)
                {
                    var remainingModel = remainingModels[i];
                    bool bDependenciesSatisfied = true;
                    foreach (var dependency in remainingModel.Dependencies)
                    {
                        if (!orderedModels.Any(m => m.NameVersion.ModelUri == dependency))
                        {
                            bDependenciesSatisfied = false;
                            break;
                        }
                    }
                    if (bDependenciesSatisfied)
                    {
                        orderedModels.Add(remainingModel);
                        remainingModels.RemoveAt(i);
                        modelAdded = true;
                    }
                    else
                    {
                        i++;
                    }
                }
            } while (remainingModels.Count > 0 && modelAdded);

            orderedModels.AddRange(remainingModels); // Add any remaining models (dependencies not satisfied, not ordered)
            return orderedModels;
        }
    }
}
