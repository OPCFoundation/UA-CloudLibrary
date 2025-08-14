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
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;

namespace AdminShell
{
    public class NodesetFileNodeManager : CustomNodeManager2
    {
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

        public void AddNamespace(string nodesetXml)
        {
            if (!string.IsNullOrEmpty(nodesetXml))
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(nodesetXml)))
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

        public void AddNodesAndValues(string nodesetXml, string values)
        {
            if (!string.IsNullOrEmpty(nodesetXml))
            {
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(nodesetXml)))
                {
                    // import nodes
                    UANodeSet nodeSet = UANodeSet.Read(stream);
                    NodeStateCollection predefinedNodes = new NodeStateCollection();
                    nodeSet.Import(SystemContext, predefinedNodes);

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
                    Server.NodeManager.GetManagerHandle(ObjectIds.ObjectsFolder, out INodeManager objectsFolderNodeManager);
                    string namespaceUri = nodeSet.NamespaceUris[0];

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
                                    List<IReference> references = new() {
                                        new NodeStateReference(
                                            ReferenceTypeIds.Organizes,
                                            false,
                                            new NodeId(NodeId.Parse(uAObject.NodeId).Identifier,
                                            (ushort)Server.NamespaceUris.GetIndex(namespaceUri))
                                        )
                                    };

                                    Dictionary<NodeId, IList<IReference>> dictionary = new() {
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
                        if (!string.IsNullOrEmpty(values))
                        {
                            Dictionary<string, string> keyvalues = JsonConvert.DeserializeObject<Dictionary<string, string>>(values);
                            foreach (KeyValuePair<string, string> value in keyvalues)
                            {
                                NodeId nodeId = new NodeId(NodeId.Parse(value.Key).Identifier, (ushort)Server.NamespaceUris.GetIndex(namespaceUri));
                                if (Find(nodeId) is BaseVariableState variable)
                                {
                                    variable.Value = new Variant(value.Value);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing values JSON. Skipping values patching:" + ex.Message);
                    }
                }
            }
        }
    }
}
