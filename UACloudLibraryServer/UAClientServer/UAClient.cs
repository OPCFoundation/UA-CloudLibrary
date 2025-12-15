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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Configuration;

namespace AdminShell
{
    public class UAClient : IAsyncDisposable
    {
        public List<string> LoadedNamespaces => ((NodesetFileNodeManager)_server?.CurrentInstance.NodeManager.NodeManagers[2])?.NamespaceUris.ToList() ?? new List<string>();

        public List<string> MissingNamespaces { get; private set; } = new List<string>();

        private readonly ApplicationInstance _app;
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;

        private SimpleServer _server;
        private Opc.Ua.Client.ISession _session;
        private SessionReconnectHandler _reconnectHandler;

        private static uint _port = 5000;
        private static ConcurrentDictionary<string, Opc.Ua.Client.ISession> _sessions = new();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public UAClient(ApplicationInstance app, DbFileStorage storage, CloudLibDataProvider database)
        {
            _app = app;
            _storage = storage;
            _database = database;
        }

        public async Task<List<NodesetViewerNode>> GetChildren(string userId, string nodesetIdentifier, string nodeId)
        {
            List<NodesetViewerNode> nodes = null;
            ReferenceDescriptionCollection references = null;

            try
            {
                if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
                {
                    return null;
                }

                BrowseDescription nodeToBrowse = new() {
                    NodeId = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable | NodeClass.ObjectType),
                    ResultMask = (uint)BrowseResultMask.All
                };

                references = await Browse(_session, nodeToBrowse).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetChildren: " + ex.Message);

                await DisposeSession().ConfigureAwait(false);
            }

            if ((references != null) && (references.Count > 0))
            {
                nodes = new List<NodesetViewerNode>();

                foreach (ReferenceDescription description in references)
                {
                    NodeId id = ExpandedNodeId.ToNodeId(description.NodeId, _session.NamespaceUris);

                    nodes.Add(new NodesetViewerNode() {
                        Id = NodeId.ToExpandedNodeId(id, _session.NamespaceUris).ToString(),
                        Text = description.DisplayName.ToString(),
                        Children = new List<NodesetViewerNode>()
                    });
                }
            }

            return nodes;
        }

        public async Task<Dictionary<string, string>> BrowseVariableNodesResursivelyAsync(string userId, string nodesetIdentifier, string nodeId)
        {
            Dictionary<string, string> results = new();

            if (nodeId == null)
            {
                nodeId = ObjectIds.ObjectsFolder.ToString();
            }

            ReferenceDescriptionCollection references = null;

            try
            {
                if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
                {
                    return null;
                }

                BrowseDescription nodeToBrowse = new BrowseDescription {
                    NodeId = nodeId,
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(NodeClass.Object | NodeClass.Variable),
                    ResultMask = (uint)BrowseResultMask.All
                };

                references = await Browse(_session, nodeToBrowse).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BrowseVariableNodesResursivelyAsync: " + ex.Message);

                await DisposeSession().ConfigureAwait(false);
            }

            if ((references != null) && (references.Count > 0))
            {
                foreach (ReferenceDescription description in references)
                {
                    // skip nodes from default namespace
                    if (description.NodeId.NamespaceIndex == 0)
                    {
                        continue; // skip default namespace
                    }

                    if (description.NodeClass == NodeClass.Variable)
                    {
                        try
                        {
                            string value = await VariableRead(userId, nodesetIdentifier, description.NodeId.ToString()).ConfigureAwait(false);
                            if (!string.IsNullOrEmpty(value))
                            {
                                results.Add(description.NodeId.ToString(), value);
                            }
                        }
                        catch (Exception)
                        {
                            // skip this node
                        }
                    }

                    // recursively browse child variable nodes
                    Dictionary<string, string> childResults = await BrowseVariableNodesResursivelyAsync(userId, nodesetIdentifier, description.NodeId.ToString()).ConfigureAwait(false);
                    if (childResults != null)
                    {
                        foreach (KeyValuePair<string, string> kvp in childResults)
                        {
                            if (!results.ContainsKey(kvp.Key))
                            {
                                results.Add(kvp.Key, kvp.Value);
                            }
                        }
                    }
                }
            }

            return results;
        }

        private async Task LoadDependentNodesetsRecursiveAsync(string userId, string nodesetIdentifier, NodesetFileNodeManager nodeManager)
        {
            NodeSetModel nodeSetMeta = await _database.GetNodeSets(userId, nodesetIdentifier).FirstOrDefaultAsync().ConfigureAwait(false);
            if ((nodeSetMeta != null) && (nodeSetMeta.RequiredModels != null) && (nodeSetMeta.RequiredModels.Count > 0))
            {
                foreach (RequiredModelInfoModel requiredModel in nodeSetMeta.RequiredModels)
                {
                    if (requiredModel.ModelUri == "http://opcfoundation.org/UA/")
                    {
                        // skip the base UA nodeset as it is always loaded
                        continue;
                    }

                    if (nodeManager.NamespaceUris.Contains(requiredModel.ModelUri))
                    {
                        // the dependent model is already loaded
                        continue;
                    }

                    // check if we have the required model in the database
                    List<NodeSetModel> matchingNodeSets = await _database.GetNodeSets(userId, null, requiredModel.ModelUri).ToListAsync().ConfigureAwait(false);
                    if (matchingNodeSets == null || matchingNodeSets.Count == 0)
                    {
                        Console.WriteLine($"Required model {requiredModel.ModelUri} for {nodesetIdentifier} not found in database.");
                        MissingNamespaces.Add(requiredModel.ModelUri);
                        continue;
                    }

                    NodeSetModel dependentNodeset = NodeModelUtils.GetMatchingOrHigherNodeSet(matchingNodeSets, requiredModel.PublicationDate, requiredModel.Version);

                    await LoadDependentNodesetsRecursiveAsync(userId, dependentNodeset.Identifier, nodeManager).ConfigureAwait(false);

                    DbFiles nodesetXml = await _storage.DownloadFileAsync(dependentNodeset.Identifier).ConfigureAwait(false);
                    nodeManager.AddNamespace(nodesetXml.Blob);
                    nodeManager.AddNodesAndValues(nodesetXml.Blob, nodesetXml.Values);
                }
            }
        }

        private void Client_ReconnectComplete(object sender, EventArgs e)
        {
            // ignore callbacks from discarded objects.
            if (!Object.ReferenceEquals(sender, _reconnectHandler))
            {
                return;
            }

            _session = (Opc.Ua.Client.Session)_reconnectHandler.Session;
            _reconnectHandler.Dispose();
            _reconnectHandler = null;

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "--- RECONNECTED --- {0}", _session.Endpoint.EndpointUrl));
        }

        private void StandardClient_KeepAlive(ISession sender, KeepAliveEventArgs e)
        {
            if (e != null && sender != null)
            {
                // ignore callbacks from discarded objects.
                if (!Object.ReferenceEquals(sender, _session))
                {
                    return;
                }

                if (!ServiceResult.IsGood(e.Status))
                {
                    Console.WriteLine(String.Format(
                        CultureInfo.InvariantCulture,
                        "Status: {0} Outstanding requests: {1} Defunct requests: {2}",
                        e.Status,
                        sender.OutstandingRequestCount,
                        sender.DefunctRequestCount));

                    if (e.Status.StatusCode == StatusCodes.BadNoCommunication && _reconnectHandler == null)
                    {
                        Console.WriteLine("--- RECONNECTING --- {0}", sender.Endpoint.EndpointUrl);
                        _reconnectHandler = new SessionReconnectHandler();
                        _reconnectHandler.BeginReconnect(sender, 10000, Client_ReconnectComplete);
                    }
                }
            }
        }

        private async Task<ReferenceDescriptionCollection> Browse(Opc.Ua.Client.ISession session, BrowseDescription nodeToBrowse)
        {
            (ResponseHeader responseHeader,
            Byte[] continuationPoints,
            ReferenceDescriptionCollection referencesList) = await session.BrowseAsync(
                null,
                null,
                nodeToBrowse.NodeId,
                0,
                BrowseDirection.Forward,
                null,
                true,
                nodeToBrowse.NodeClassMask
                ).ConfigureAwait(false);

            do
            {
                if (referencesList.Count == 0 || continuationPoints == null)
                {
                    break;
                }

                (ResponseHeader responseHeaderNext,
                Byte[] continuationPointsNext,
                ReferenceDescriptionCollection referencesListNext) = await session.BrowseNextAsync(
                    null,
                    false,
                    continuationPoints
                ).ConfigureAwait(false);

                if (referencesListNext.Count > 0)
                {
                    // append results to the master list
                    referencesList.AddRange(referencesListNext);
                }

                if (continuationPointsNext == null)
                {
                    break;
                }
                else
                {
                    continuationPoints = continuationPointsNext;
                }
            }
            while (true);

            return referencesList;
        }

        public async Task<object> GetTypeDefinition(string userId, string nodesetIdentifier, string nodeId)
        {
            if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
            {
                return string.Empty;
            }

            // read the variable node from the OPC UA server
            Node node = await _session.ReadNodeAsync(ExpandedNodeId.ToNodeId(new ExpandedNodeId(nodeId), _session.NamespaceUris)).ConfigureAwait(false);
            if (node?.NodeClass == NodeClass.DataType)
            {
                // return complex type definition
                DataTypeNode dataTypeNode = (DataTypeNode)node;
                return new { Namespaces = _session.NamespaceUris.ToArray(), dataTypeNode.DataTypeDefinition };
            }
            else
            {
                // return references
                ReferenceDescriptionCollection references = await Browse(_session, new BrowseDescription {
                    NodeId = ExpandedNodeId.ToNodeId(new ExpandedNodeId(nodeId), _session.NamespaceUris),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = null,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.All
                }).ConfigureAwait(false);
                return new { Namespaces = _session.NamespaceUris.ToArray(), references };
            }
        }

        public async Task<string> VariableRead(string userId, string nodesetIdentifier, string nodeId)
        {
            string value = string.Empty;

            try
            {
                ReadValueIdCollection nodesToRead = new();

                if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
                {
                    return string.Empty;
                }

                // read the variable node from the OPC UA server
                VariableNode node = (VariableNode)await _session.ReadNodeAsync(ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris)).ConfigureAwait(false);

                ReadValueId valueId = new();
                valueId.NodeId = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris);
                valueId.AttributeId = Attributes.Value;
                valueId.IndexRange = null;
                valueId.DataEncoding = null;
                nodesToRead.Add(valueId);

                // load complex type system
                ComplexTypeSystem complexTypeSystem = new(_session);
                ExpandedNodeId nodeTypeId = node.DataType;
                await complexTypeSystem.LoadTypeAsync(nodeTypeId).ConfigureAwait(false);

                ReadResponse response = await _session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead, CancellationToken.None).ConfigureAwait(false);

                ClientBase.ValidateResponse(response.Results, nodesToRead);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToRead);

                if ((response.Results.Count > 0) && (response.Results[0].Value != null))
                {
                    value = response.Results[0].ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("VariableRead: " + ex.Message);
            }

            return value;
        }

        public async Task VariableWrite(string userId, string nodesetIdentifier, string nodeId, string payload)
        {
            try
            {
                if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
                {
                    return;
                }

                NodeId nodeID = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris);

                WriteValue nodeToWrite = new() {
                    NodeId = nodeID,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(payload)
                };

                WriteValueCollection nodesToWrite = new() {
                    nodeToWrite
                };

                WriteResponse response = await _session.WriteAsync(null, nodesToWrite, CancellationToken.None).ConfigureAwait(false);

                ClientBase.ValidateResponse(response.Results, nodesToWrite);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToWrite);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw ServiceResultException.Create(response.Results[0], 0, response.DiagnosticInfos, response.ResponseHeader.StringTable);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing OPC UA node failed: " + ex.Message);
            }
        }

        private async Task<bool> ValidateSession(string userId, string nodesetIdentifier)
        {
            if (string.IsNullOrEmpty(nodesetIdentifier))
            {
                Console.WriteLine("Failed to validate session: No nodeset identifier specified.");
                return false;
            }

            // check if we have an existing session for this nodeset
            if (_session == null || !_session.Connected)
            {
                EndpointDescription selectedEndpoint = null;

                if (_sessions.TryGetValue(nodesetIdentifier, out Opc.Ua.Client.ISession value) && (value != null) && value.Connected)
                {
                    Console.WriteLine("Re-using existing OPC UA server and session for nodeset " + nodesetIdentifier);
                    _session = value;
                }
                else
                {
                    Console.WriteLine("Starting new OPC UA server and session for nodeset " + nodesetIdentifier);

                    await _lock.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        int maxRetry = 5000;
                        while (selectedEndpoint == null)
                        {
                            if (maxRetry < 1)
                            {
                                Console.WriteLine("Failed to start OPC UA server: Max retries reached!");
                                return false;
                            }

                            try
                            {
                                Console.WriteLine("Starting OPC UA server on port " + _port);

                                _server = new SimpleServer(_app, _port);

                                await _app.StartAsync(_server).ConfigureAwait(false);

                                selectedEndpoint = await CoreClientUtils.SelectEndpointAsync(_app.ApplicationConfiguration, "opc.tcp://localhost:" + _port, true).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Failed to establish an OPC UA server connection on port " + _port + ": " + ex.Message);
                            }

                            _port++;

                            if (_port > 10000)
                            {
                                _port = 5000;
                            }

                            maxRetry--;
                        }

                        NodesetFileNodeManager nodeManager = (NodesetFileNodeManager)_server.CurrentInstance.NodeManager.NodeManagers[2];

                        // first load dependencies
                        await LoadDependentNodesetsRecursiveAsync(userId, nodesetIdentifier, nodeManager).ConfigureAwait(false);

                        // now load the nodeset itself
                        DbFiles nodesetXml = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
                        if (nodesetXml != null)
                        {

                            nodeManager.AddNamespace(nodesetXml.Blob);
                            nodeManager.AddNodesAndValues(nodesetXml.Blob, nodesetXml.Values);
                        }
                        else
                        {
                            Console.WriteLine($"Nodeset {nodesetIdentifier} not found in database.");
                            return false;
                        }

                        ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(_app.ApplicationConfiguration));
                        Opc.Ua.Client.ISession newSession = await DefaultSessionFactory.Instance.CreateAsync(
                            _app.ApplicationConfiguration,
                            configuredEndpoint,
                            true,
                            false,
                            string.Empty,
                            30000,
                            new UserIdentity(new AnonymousIdentityToken()),
                            null).ConfigureAwait(false);

                        if (newSession == null || !newSession.Connected)
                        {
                            Console.WriteLine("Failed to create new OPC UA session.");
                            return false;
                        }

                        newSession.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);

                        await newSession.FetchNamespaceTablesAsync().ConfigureAwait(false);

                        _sessions[nodesetIdentifier] = newSession;
                        _session = newSession;

                        Console.WriteLine("OPC UA server started and session created for nodeset " + nodesetIdentifier);
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            }

            return true;
        }

        public async Task<string> CopyNodeset(string userId, string nodesetIdentifier, string name)
        {
            try
            {
                DbFiles file = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);

                UANameSpace metadata = await _database.RetrieveAllMetadataAsync(userId, uint.Parse(nodesetIdentifier, CultureInfo.InvariantCulture)).ConfigureAwait(false);

                metadata.Title = name;
                metadata.Nodeset.NodesetXml = file.Blob;

                // update publication date in nodeset XML with current date in UTC format
                const string key = "PublicationDate=\"";
                int keyIndex = metadata.Nodeset.NodesetXml.IndexOf(key, StringComparison.Ordinal);
                int start = keyIndex + key.Length;
                string now = DateTime.UtcNow.Date.ToString("yyyy-MM-ddTHH:mm:ss'Z'", CultureInfo.InvariantCulture);

                var sb = new StringBuilder(metadata.Nodeset.NodesetXml);
                sb.Remove(start, now.Length);
                sb.Insert(start, now);
                metadata.Nodeset.NodesetXml = sb.ToString();

                return await _database.UploadNamespaceAndNodesetAsync(userId, metadata, file.Values, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CopyNodeset: " + ex.Message);
                return ex.Message;
            }
        }

        public async Task DisposeSession()
        {
            // remove session from our concurrent dictionary
            foreach (var key in _sessions.Keys.ToList())
            {
                if (_sessions[key] == _session)
                {
                    _sessions.TryRemove(key, out _);
                }
            }

            if (_session != null)
            {
                if (_session.Connected)
                {
                    await _session.CloseAsync().ConfigureAwait(false);
                }

                _session.Dispose();
                _session = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            // remove session from our concurrent dictionary
            await DisposeSession().ConfigureAwait(false);

            if (_reconnectHandler != null)
            {
                _reconnectHandler.Dispose();
                _reconnectHandler = null;
            }

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }
        }
    }
}
