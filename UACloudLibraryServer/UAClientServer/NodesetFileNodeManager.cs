using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Export;
using Opc.Ua.Server;

namespace AdminShell
{
    public class NodesetFileNodeManager : CustomNodeManager2
    {
        private string _nodesetXml = string.Empty;

        public NodesetFileNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            NamespaceUris = new List<string>();
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            IList<IReference> objectsFolderReferences = null;
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out objectsFolderReferences))
            {
                externalReferences[ObjectIds.ObjectsFolder] = objectsFolderReferences = new List<IReference>();
            }
        }

        public async Task AddNamespace(DbFileStorage storage, string nodesetIdentifier)
        {
            _nodesetXml = await storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(_nodesetXml))
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_nodesetXml)))
                {
                    UANodeSet nodeSet = UANodeSet.Read(stream);

                    if ((nodeSet.NamespaceUris != null) && (nodeSet.NamespaceUris.Length > 0))
                    {
                        List<string> newNamespaceUris = nodeSet.NamespaceUris.ToList();
                        List<string> existingNamespaceUris = NamespaceUris.ToList();

                        foreach (string ns in newNamespaceUris)
                        {
                            if (!existingNamespaceUris.Contains(ns))
                            {
                                lock (Lock)
                                {
                                    existingNamespaceUris.Add(ns);

                                    // update the table used by this NodeManager
                                    SetNamespaces(existingNamespaceUris.ToArray());

                                    // register the new URI with the MasterNodeManager
                                    Server.NodeManager.RegisterNamespaceManager(ns, this);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void AddNodesAndValues()
        {
            if (!string.IsNullOrEmpty(_nodesetXml))
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(_nodesetXml)))
                {
                    // import nodes
                    UANodeSet nodeSet = UANodeSet.Read(stream);
                    NodeStateCollection predefinedNodes = new NodeStateCollection();
                    nodeSet.Import(SystemContext, predefinedNodes);
                    Server.NodeManager.GetManagerHandle(ObjectIds.ObjectsFolder, out INodeManager objectsFolderNodeManager);
                    string namespaceUri = nodeSet.NamespaceUris[0];

                    // add nodes
                    for (int i = 0; i < predefinedNodes.Count; i++)
                    {
                        try
                        {
                            AddPredefinedNode(SystemContext, predefinedNodes[i]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message, ex);
                        }
                    }

                    // add references for our top-level nodes to the objects folder
                    foreach (UANode node in nodeSet.Items)
                    {
                        if (node is UAObject uAObject)
                        {
                            if ((uAObject.ParentNodeId == ObjectIds.ObjectsFolder)
                             || (uAObject.References.Where(r => (r.ReferenceType == "Organizes") && (r.Value == ObjectIds.ObjectsFolder)).ToList().Count > 0))
                            {
                                if (((node.DisplayName != null) && (node.DisplayName.Length > 0) && (node.DisplayName[0].Value == "Submodels"))
                                 || (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GENERATE_FULL_AAS"))))
                                {
                                    List<IReference> references = new()
                                    {
                                    new NodeStateReference(ReferenceTypeIds.Organizes, false, new NodeId(NodeId.Parse(uAObject.NodeId).Identifier, (ushort)Server.NamespaceUris.GetIndex(namespaceUri)))
                                };

                                    Dictionary<NodeId, IList<IReference>> dictionary = new()
                                    {
                                    { ObjectIds.ObjectsFolder, references }
                                };

                                    objectsFolderNodeManager.AddReferences(dictionary);
                                }
                            }
                        }
                    }

                    // patch the values from our values file
                    try
                    {
                        //TODO: Implement a way to load values from the database
                        //string valuesFile = Path.Combine(nodesetFile.Replace(".NodeSet2.xml", "_Values.json"));
                        //if (System.IO.File.Exists(valuesFile))
                        //{
                        //    Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(valuesFile));
                        //    foreach (KeyValuePair<string, string> value in values)
                        //    {
                        //        NodeId nodeId = new NodeId(NodeId.Parse(value.Key).Identifier, (ushort)Server.NamespaceUris.GetIndex(namespaceUri));
                        //        if (Find(nodeId) is BaseVariableState variable)
                        //        {
                        //            variable.Value = new Variant(value.Value);
                        //        }
                        //    }
                        //}
                    }
                    catch (Exception)
                    {
                        // skip loading values
                    }
                }
            }
        }
    }
}
