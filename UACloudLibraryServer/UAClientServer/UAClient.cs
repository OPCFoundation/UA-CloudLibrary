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
using System.Xml.Linq;
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
    public class UAClient : IDisposable
    {
        public List<string> LoadedNamespaces => ((NodesetFileNodeManager)_server?.CurrentInstance.NodeManager.NodeManagers[2])?.NamespaceUris.ToList() ?? new List<string>();

        public List<string> MissingNamespaces { get; private set; } = new List<string>();

        private readonly ApplicationInstance _app;
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;

        private SimpleServer _server;
        private Opc.Ua.Client.Session _session;
        private SessionReconnectHandler _reconnectHandler;

        private static uint _port = 5000;
        private static ConcurrentDictionary<string, Opc.Ua.Client.Session> _sessions = new();

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

                references = Browse(_session, nodeToBrowse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetChildren: " + ex.Message);
                DisposeSession(); //CM: Must go through dispose to remove session from _session table.
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

                references = Browse(_session, nodeToBrowse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BrowseVariableNodesResursivelyAsync: " + ex.Message);
                DisposeSession();
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

        private async Task<Opc.Ua.Client.Session> CreateSessionAsync(string userId, string nodesetIdentifier)
        {
            if (_sessions.TryGetValue(nodesetIdentifier, out Opc.Ua.Client.Session value) && (value != null) && value.Connected)
            {
                return value;
            }

            int cMaxRetry = 1000;
            EndpointDescription selectedEndpoint = null;
            while (selectedEndpoint == null)
            {
                try
                {
                    _server = new SimpleServer(_app, _port);

                    await _server.StartServerAsync().ConfigureAwait(false);

                    selectedEndpoint = CoreClientUtils.SelectEndpoint(_app.ApplicationConfiguration, "opc.tcp://localhost:" + _port, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to establish an OPC UA server connection on port " + _port + ": " + ex.Message);
                }

                lock (_app)
                {
                    _port++;

                    if (_port > 10000)
                    {
                        _port = 5000;
                    }
                }

                cMaxRetry--;
                if (cMaxRetry < 1)
                    break;
            }

            if (!String.IsNullOrEmpty(nodesetIdentifier))
            {
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
                    Console.WriteLine($"Required model for {nodesetIdentifier} not found in database.");
                }
            }

            // Even if we cannot load the nodeset, attempt to create a session as the user requests.
            ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(_app.ApplicationConfiguration));
            Opc.Ua.Client.Session newSession = await Opc.Ua.Client.Session.Create(
                    _app.ApplicationConfiguration,
                    configuredEndpoint,
                    true,
                    false,
                    string.Empty,
                    30000,
                    new UserIdentity(new AnonymousIdentityToken()),
                    null).ConfigureAwait(false);

            newSession.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);

            newSession.FetchNamespaceTables();

            _sessions[nodesetIdentifier] = newSession;

            return newSession;
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
                    List<NodeSetModel> matchingNodeSets = await _database.NodeSets
                        .Where(nsm => nsm.ModelUri == requiredModel.ModelUri && ((userId == "admin") || (nsm.Metadata.UserId == userId) || string.IsNullOrEmpty(nsm.Metadata.UserId)))
                        .ToListAsync()
                        .ConfigureAwait(false);

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

        private ReferenceDescriptionCollection Browse(Opc.Ua.Client.Session session, BrowseDescription nodeToBrowse)
        {
            ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();

            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection
            {
                nodeToBrowse
            };

            session.Browse(
            null,
            null,
            0,
            nodesToBrowse,
            out BrowseResultCollection results,
            out DiagnosticInfoCollection diagnosticInfos);

            ClientBase.ValidateResponse(results, nodesToBrowse);
            ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToBrowse);

            do
            {
                if (StatusCode.IsBad(results[0].StatusCode))
                {
                    break;
                }

                for (int i = 0; i < results[0].References.Count; i++)
                {
                    references.Add(results[0].References[i]);
                }

                if (results[0].References.Count == 0 || results[0].ContinuationPoint == null)
                {
                    break;
                }

                ByteStringCollection continuationPoints = new ByteStringCollection {
                    results[0].ContinuationPoint
                };

                session.BrowseNext(
                    null,
                    false,
                    continuationPoints,
                    out results,
                    out diagnosticInfos);

                ClientBase.ValidateResponse(results, continuationPoints);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, continuationPoints);
            }
            while (true);

            return references;
        }

        public async Task<string> VariableRead(string userId, string nodesetIdentifier, string nodeId)
        {
            string value = string.Empty;

            try
            {
                DataValueCollection values = null;
                DiagnosticInfoCollection diagnosticInfos = null;
                ReadValueIdCollection nodesToRead = new();

                if (!await ValidateSession(userId, nodesetIdentifier).ConfigureAwait(false))
                {
                    return string.Empty;
                }

                // read the variable node from the OPC UA server
                VariableNode node = (VariableNode)_session.ReadNode(ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris));

                ReadValueId valueId = new();
                valueId.NodeId = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris);
                valueId.AttributeId = Attributes.Value;
                valueId.IndexRange = null;
                valueId.DataEncoding = null;
                nodesToRead.Add(valueId);

                // load complex type system
                ComplexTypeSystem complexTypeSystem = new(_session);
                ExpandedNodeId nodeTypeId = node.DataType;
                await complexTypeSystem.LoadType(nodeTypeId).ConfigureAwait(false);

                ResponseHeader responseHeader = _session.Read(null, 0, TimestampsToReturn.Both, nodesToRead, out values, out diagnosticInfos);

                ClientBase.ValidateResponse(values, nodesToRead);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToRead);

                if ((values.Count > 0) && (values[0].Value != null))
                {
                    value = values[0].ToString();
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

                StatusCodeCollection results = null;
                DiagnosticInfoCollection diagnosticInfos = null;

                ResponseHeader responseHeader = _session.Write(
                    null,
                    nodesToWrite,
                    out results,
                    out diagnosticInfos);

                ClientBase.ValidateResponse(results, nodesToWrite);
                ClientBase.ValidateDiagnosticInfos(diagnosticInfos, nodesToWrite);

                if (StatusCode.IsBad(results[0]))
                {
                    throw ServiceResultException.Create(results[0], 0, diagnosticInfos, responseHeader.StringTable);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing OPC UA node failed: " + ex.Message);
            }
        }

        private readonly SemaphoreSlim SessionSemaphoreSlim = new SemaphoreSlim(1, 1);
        private async Task<bool> ValidateSession(string userId, string nodesetIdentifier)
        {
            await SessionSemaphoreSlim.WaitAsync();
            try
            {
                if (_session == null || !_session.Connected)
                {
                    _session = await CreateSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                    if (_session == null || !_session.Connected)
                    {
                        Console.WriteLine("Failed to create OPC UA session.");
                        return false;
                    }
                }
            }
            finally
            {
                SessionSemaphoreSlim.Release();
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

        public void DisposeSession()
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
                    _session.Close();
                }
                _session.Dispose();
                _session = null;
            }
        }

        public void Dispose()
        {
            // remove session from our concurrent dictionary
            DisposeSession();

            if (_reconnectHandler != null)
            {
                _reconnectHandler.Dispose();
                _reconnectHandler = null;
            }

            if (_server != null)
            {
                _app.Stop();
                _server = null;
            }
        }
    }
}
