/* Author:      Chris Muench, C-Labs
 * Last Update: 4/8/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2021
 */
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Opc.Ua.Export;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace CESMII.OpcUa.NodeSetImporter
{

    /// <summary>
    /// Implementation of File Cache - can be replaced with Database cache if necessary
    /// </summary>
    public class UANodeSetFileCache : IUANodeSetCache
    {
        public UANodeSetFileCache()
        {
            RootFolder = Path.Combine(Directory.GetCurrentDirectory(), "NodeSetCache");
        }

        public UANodeSetFileCache(string pRootFolder)
        {
            RootFolder = pRootFolder;
        }
        static string RootFolder = null;
        /// <summary>
        /// Not Supported on File Cache
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ModelValue GetNodeSetByID(string id)
        {
            return null;
        }

        /// <summary>
        /// By default the Imporater caches all imported NodeSets in a directory called "/NodeSets" under the correct bin directory
        /// This function can be called to flush this cache (for debugging and development purpose only!)
        /// </summary>
        /// <returns></returns>
        public UANodeSetImportResult FlushCache()
        {
            UANodeSetImportResult ret = new UANodeSetImportResult();
            string tPath = Path.Combine(RootFolder, "NodeSets");
            try
            {
                var tFiles = Directory.GetFiles(tPath);
                foreach (var tfile in tFiles)
                {
                    File.Delete(tfile);
                }
            }
            catch (Exception e)
            {
                ret.ErrorMessage = $"Flushing Cache failed: {e}";
            }
            return ret;
        }

        /// <summary>
        /// After the NodeSets were returned by the Importer the succeeding code might fail during processing.
        /// This function allows to remove NodeSets from the cache if the succeeding call failed
        /// </summary>
        /// <param name="results">Set to the result-set coming from the ImportNodeSets message to remove newly added NodeSets from the cache</param>
        public void DeleteNewlyAddedNodeSetsFromCache(UANodeSetImportResult results)
        {
            if (results?.Models?.Count > 0)
            {
                foreach (var tMod in results.Models)
                {
                    if (tMod.NewInThisImport)
                        File.Delete(tMod.FilePath);
                }
            }
        }

        /// <summary>
        /// Returns the content of a cached NodeSet
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string GetRawModelXML(ModelValue model)
        {
            if (!File.Exists(model?.FilePath))
                return null;
            return File.ReadAllText(model.FilePath);
        }

        /// <summary>
        /// Loads a NodeSet From File.
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodesetFileName"></param>
        public void AddNodeSetFile(UANodeSetImportResult results, string nodesetFileName, object tenantId)
        {
            if (!File.Exists(nodesetFileName))
                return;
            var nodeSetXml = File.ReadAllText(nodesetFileName);
            AddNodeSet(results, nodeSetXml, tenantId, false);
        }

        public bool GetNodeSet(UANodeSetImportResult results, ModelNameAndVersion nameVersion, object TenantID)
        {
            //Try to find already uploaded NodeSets using cached NodeSets in the "NodeSets" Folder.
            string tFileName = GetCacheFileName(nameVersion, TenantID);
            if (File.Exists(tFileName))
            {
                AddNodeSetFile(results, tFileName, TenantID);
                return true;
            }
            return false;
        }

        private static string GetCacheFileName(ModelNameAndVersion nameVersion, object TenantID)
        {
            string tPath = Path.Combine(RootFolder, "NodeSets");
            if (!Directory.Exists(tPath))
                Directory.CreateDirectory(tPath);
            if (TenantID != null && (int)TenantID > 0)
            {
                tPath = Path.Combine(tPath, $"{(int)TenantID}");
                if (!Directory.Exists(tPath))
                    Directory.CreateDirectory(tPath);
            }
            string tFile = nameVersion.ModelUri.Replace("http://", "");
            tFile = tFile.Replace('/', '.');
            if (!tFile.EndsWith(".")) tFile += ".";
            string filePath = Path.Combine(tPath, $"{tFile}NodeSet2.xml");
            return filePath;
        }

        /// <summary>
        /// Loads NodeSets from a given byte array and saves new NodeSets to the cache
        /// </summary>
        /// <param name="results"></param>
        /// <param name="nodesetArray"></param>

        /// <returns></returns>
        public bool AddNodeSet(UANodeSetImportResult results, string nodeSetXml, object TenantID, bool requested)
        {
            bool WasNewSet = false;
            // UANodeSet.Read disposes the stream. We need it later on so create a copy
            UANodeSet nodeSet;

            // workaround for bug https://github.com/dotnet/runtime/issues/67622
            var patchedXML = nodeSetXml.Replace("<Value/>", "<Value xsi:nil='true' />");
            using (var nodesetBytes = new MemoryStream(Encoding.UTF8.GetBytes(patchedXML)))
            {
                nodeSet = UANodeSet.Read(nodesetBytes);
            }

            #region Comment processing
            var headerComment = NodeModelUtils.ReadHeaderComment(patchedXML);
            #endregion

            UANodeSet tOldNodeSet = null;
            if (nodeSet?.Models == null)
            {
                results.ErrorMessage = $"No Nodeset found in bytes";
                return false;
            }
            foreach (var importedModel in nodeSet.Models)
            {
                //Caching the streams to a "NodeSets" subfolder using the Model Name
                //Even though "Models" is an array, most NodeSet files only contain one model.
                //In case a NodeSet stream does contain multiple models, the same file will be cached with each Model Name 
                string filePath = GetCacheFileName(new ModelNameAndVersion(importedModel), TenantID);

                bool CacheNewerVersion = true;
                if (File.Exists(filePath))
                {
                    CacheNewerVersion = false;
                    using (Stream nodeSetStream = new FileStream(filePath, FileMode.Open))
                    {
                        if (tOldNodeSet == null)
                            tOldNodeSet = UANodeSet.Read(nodeSetStream);
                    }
                    var tOldModel = tOldNodeSet.Models.Where(s => s.ModelUri == importedModel.ModelUri).OrderByDescending(s => s.GetNormalizedPublicationDate()).FirstOrDefault();
                    if (tOldModel == null
                        || NodeSetVersionUtils.CompareNodeSetVersion(
                                importedModel.ModelUri,
                                importedModel.GetNormalizedPublicationDate(), importedModel.Version,
                                tOldModel.GetNormalizedPublicationDate(), tOldModel.Version) > 0)
                    {
                        CacheNewerVersion = true; //Cache the new NodeSet if the old (file) did not contain the model or if the version of the new model is greater
                    }
                }
                if (CacheNewerVersion) //Cache only newer version
                {
                    File.WriteAllText(filePath, nodeSetXml);
                    WasNewSet = true;
                }
                var modelInfo = results.AddModelAndDependencies(nodeSet, headerComment, importedModel, filePath, WasNewSet);
                modelInfo.Model.RequestedForThisImport = requested;
            }
            return WasNewSet;
        }
    }
}
