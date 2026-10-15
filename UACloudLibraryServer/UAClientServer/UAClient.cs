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
using Microsoft.Extensions.Logging;
using Opc.Ua.Client;
using Opc.Ua.Client.ComplexTypes;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Configuration;

namespace Opc.Ua.Cloud.Library
{
    public class UAClient : IAsyncDisposable
    {
        public Dictionary<string, Tuple<string, string>> LoadedNamespaces { get; private set; } = new();

        public List<string> MissingNamespaces { get; private set; } = new();

        private readonly ApplicationInstance _app;
        private readonly DbFileStorage _storage;
        private readonly CloudLibDataProvider _database;

        private SimpleServer _server;

        // A session and the embedded server it is connected to share one lifetime. Clients borrow the
        // pair from the static cache, and it is closed and stopped only when its last borrower
        // releases it - never by whichever client happened to create it - so disposing one client
        // cannot pull a session out from under another that is still browsing it.
        // The pair also owns its keepalive and reconnect, so they keep working for every borrower no
        // matter which client created the pair or has since released it.
        private sealed class SharedSession : IDisposable
        {
            private readonly object _gate = new();
            private readonly ITelemetryContext _telemetry;
            private SessionReconnectHandler _reconnectHandler;
            private Opc.Ua.Client.ISession _session;
            private bool _closed;

            public SharedSession(Opc.Ua.Client.ISession session, SimpleServer server, ITelemetryContext telemetry)
            {
                _session = session;
                Server = server;
                _telemetry = telemetry;
                _session.KeepAlive += OnKeepAlive;
            }

            // The current session. A reconnect may replace it, so borrowers read it on every use
            // rather than caching it.
            public Opc.Ua.Client.ISession Session
            {
                get
                {
                    lock (_gate)
                    {
                        return _session;
                    }
                }
            }

            public SimpleServer Server { get; }

            // Guarded by s_poolGate.
            public int Borrowers { get; set; }

            private void OnKeepAlive(Opc.Ua.Client.ISession sender, KeepAliveEventArgs e)
            {
                if (e == null || sender == null || ServiceResult.IsGood(e.Status))
                {
                    return;
                }

                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "Status: {0} Outstanding requests: {1} Defunct requests: {2}",
                    e.Status,
                    sender.OutstandingRequestCount,
                    sender.DefunctRequestCount));

                if (e.Status.StatusCode != StatusCodes.BadNoCommunication)
                {
                    return;
                }

                lock (_gate)
                {
                    // Ignore a stale session's events, a reconnect already running, or a closed pair.
                    if (_closed || _reconnectHandler != null || !ReferenceEquals(sender, _session))
                    {
                        return;
                    }

                    Console.WriteLine("--- RECONNECTING --- {0}", sender.Endpoint.EndpointUrl);
                    _reconnectHandler = new SessionReconnectHandler(_telemetry);
                    _reconnectHandler.BeginReconnect(sender, 10000, OnReconnectComplete);
                }
            }

            private void OnReconnectComplete(object sender, EventArgs e)
            {
                lock (_gate)
                {
                    if (!ReferenceEquals(sender, _reconnectHandler))
                    {
                        return;
                    }

                    Opc.Ua.Client.ISession reconnected = _reconnectHandler.Session;
                    _reconnectHandler.Dispose();
                    _reconnectHandler = null;

                    if (_closed || reconnected == null)
                    {
                        return;
                    }

                    if (!ReferenceEquals(reconnected, _session))
                    {
                        // The handler created a replacement session; move the keepalive to it.
                        _session.KeepAlive -= OnKeepAlive;
                        reconnected.KeepAlive += OnKeepAlive;
                        _session = reconnected;
                    }

                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "--- RECONNECTED --- {0}", _session.Endpoint.EndpointUrl));
                }
            }

            // Stops any reconnect in progress. CloseAsync calls this; it is safe to call more than once.
            public void Dispose()
            {
                lock (_gate)
                {
                    _closed = true;
                    _reconnectHandler?.Dispose();
                    _reconnectHandler = null;
                }
            }

            // Closes the session and stops the server. Called once, by the last borrower.
            public async Task CloseAsync()
            {
                Dispose();

                Opc.Ua.Client.ISession session;
                lock (_gate)
                {
                    session = _session;
                    session.KeepAlive -= OnKeepAlive;
                }

                try
                {
                    if (session.Connected)
                    {
                        await session.CloseAsync().ConfigureAwait(false);
                    }

                    session.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to close OPC UA session: " + ex.Message);
                }

                await StopServerAsync(Server).ConfigureAwait(false);
            }
        }

        private static readonly object s_poolGate = new();

        // The pairs this client currently borrows, at most one per nodeset. Released on DisposeAsync,
        // or individually by DisposeSession when a pair fails.
        private readonly Dictionary<string, SharedSession> _borrowed = new(StringComparer.Ordinal);

        // Servers this client started that never produced a usable session (failed start attempts).
        // No other client can know about them, so this client stops them itself.
        private readonly List<SimpleServer> _orphanServers = new();

        // The pair this client most recently selected. Operations never read it after an await: each
        // acquires its pair through AcquireSessionAsync and uses that one throughout, because another
        // call on this client may select a different nodeset in the meantime.
        private SharedSession _current;

        // The nodeset _current was borrowed for. Each nodeset runs in its own server and address
        // space, so a session is only valid for the nodeset it was created for.
        private string _sessionNodesetIdentifier;
        private ITelemetryContext _telemetry;

        private static uint _port = 5000;
        private static ConcurrentDictionary<string, SharedSession> _sessions = new();

        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public UAClient(ApplicationInstance app, DbFileStorage storage, CloudLibDataProvider database)
        {
            _app = app;
            _storage = storage;
            _database = database;

            _telemetry = DefaultTelemetry.Create(builder => builder.AddConsole());
        }

        public async Task<List<NodesetViewerNode>> GetChildren(string userId, string nodesetIdentifier, string nodeId, bool includeTypes = false)
        {
            (_, List<NodesetViewerNode> nodes) = await TryGetChildrenAsync(userId, nodesetIdentifier, nodeId, includeTypes).ConfigureAwait(false);
            return nodes;
        }

        /// <summary>
        /// Browses a node's children. Unlike <see cref="GetChildren"/>, a failure (no session, or the
        /// browse threw) is reported separately from a successful browse that found no children, so a
        /// caller that must not act on an unreadable template can tell the two apart.
        /// </summary>
        public async Task<(bool Succeeded, List<NodesetViewerNode> Nodes)> TryGetChildrenAsync(string userId, string nodesetIdentifier, string nodeId, bool includeTypes = false)
        {
            SharedSession pair = null;
            Opc.Ua.Client.ISession session;
            ReferenceDescriptionCollection references;

            try
            {
                pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
                {
                    return (false, null);
                }

                session = pair.Session;

                uint mask = (uint)(NodeClass.Object | NodeClass.Variable);
                if (includeTypes)
                {
                    mask = mask | (uint)(NodeClass.DataType | NodeClass.VariableType | NodeClass.ObjectType);
                }

                BrowseDescription nodeToBrowse = new() {
                    NodeId = ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = mask,
                    ResultMask = (uint)BrowseResultMask.All
                };

                references = await Browse(session, nodeToBrowse).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetChildren: " + ex.Message);

                await DisposeSession(pair, nodesetIdentifier).ConfigureAwait(false);
                return (false, null);
            }

            if ((references == null) || (references.Count == 0))
            {
                // Browsed successfully and found nothing; GetChildren still returns null for this.
                return (true, null);
            }

            var nodes = new List<NodesetViewerNode>();
            foreach (ReferenceDescription description in references)
            {
                NodeId id = ExpandedNodeId.ToNodeId(description.NodeId, session.NamespaceUris);

                nodes.Add(new NodesetViewerNode() {
                    Id = NodeId.ToExpandedNodeId(id, session.NamespaceUris).ToString(),
                    Text = description.DisplayName.ToString(),
                    BrowseName = description.BrowseName?.Name ?? string.Empty,
                    NodeClass = description.NodeClass.ToString(),
                    Children = new List<NodesetViewerNode>()
                });
            }

            return (true, nodes);
        }

        /// <summary>
        /// Explains why a node id from an imported value file could not be matched against the
        /// nodeset's variable values. Returns null when the id refers to an existing Variable node
        /// (which is a valid import target even if it currently has no value), otherwise a short
        /// human readable reason.
        /// </summary>
        public async Task<string> ExplainUnmatchedNodeId(string userId, string nodesetIdentifier, string nodeId)
        {
            ExpandedNodeId expanded;
            try
            {
                expanded = ExpandedNodeId.Parse(nodeId);
            }
            catch (Exception ex)
            {
                return "the id could not be parsed: " + ex.Message;
            }

            if (expanded.IsNull)
            {
                return "the id is empty";
            }

            if (!string.IsNullOrEmpty(expanded.NamespaceUri))
            {
                if (!LoadedNamespaces.ContainsKey(expanded.NamespaceUri))
                {
                    return $"namespace '{expanded.NamespaceUri}' is not loaded for this nodeset";
                }
            }
            else if (expanded.NamespaceIndex == 0)
            {
                return "nodes in the OPC UA base namespace (ns=0) are not part of a nodeset's values";
            }

            try
            {
                SharedSession pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
                {
                    return "no session to the nodeset server";
                }

                Opc.Ua.Client.ISession session = pair.Session;
                NodeId id = ExpandedNodeId.ToNodeId(expanded, session.NamespaceUris);
                if (NodeId.IsNull(id))
                {
                    return $"namespace '{expanded.NamespaceUri}' is not in the server namespace table";
                }

                Node node = await session.ReadNodeAsync(id).ConfigureAwait(false);
                if (node == null)
                {
                    return "no node with this id exists in the nodeset";
                }

                if (node.NodeClass != NodeClass.Variable)
                {
                    return $"the node '{node.DisplayName}' is a {node.NodeClass}, not a Variable";
                }

                // An existing Variable is always a valid value set target, regardless of whether
                // it currently has a value or how it is referenced.
                return null;
            }
            catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdUnknown)
            {
                return "no node with this id exists in the nodeset";
            }
            catch (Exception ex)
            {
                return "the node could not be read: " + ex.Message;
            }
        }

        public async Task<Dictionary<string, string>> BrowseVariableNodesResursivelyAsync(string userId, string nodesetIdentifier, string nodeId)
        {
            Dictionary<string, string> results = new();

            if (nodeId == null)
            {
                nodeId = ObjectIds.ObjectsFolder.ToString();
            }

            ReferenceDescriptionCollection references = null;
            SharedSession pair = null;

            try
            {
                pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
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

                references = await Browse(pair.Session, nodeToBrowse).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("BrowseVariableNodesResursivelyAsync: " + ex.Message);

                await DisposeSession(pair, nodesetIdentifier).ConfigureAwait(false);
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
                    if (requiredModel.ModelUri == Namespaces.OpcUa)
                    {
                        if (!LoadedNamespaces.ContainsKey(Namespaces.OpcUa))
                        {
                            // Get the default namespace metadata node
                            NodeState defaultNamespace = _server.CurrentInstance.NodeManager.ConfigurationNodeManager.Find(ObjectIds.OPCUANamespaceMetadata);
                            if (defaultNamespace == null)
                            {
                                continue;
                            }

                            if (string.Equals(defaultNamespace.BrowseName.Name, Namespaces.OpcUa, StringComparison.Ordinal))
                            {
                                List<BaseInstanceState> namespaceProperty = new();
                                defaultNamespace.GetChildren(_server.CurrentInstance.DefaultSystemContext, namespaceProperty);
                                foreach (var property in namespaceProperty)
                                {
                                    if (string.Equals(property.BrowseName.Name, "NamespaceVersion", StringComparison.Ordinal))
                                    {
                                        string version = ((BaseVariableState)property).Value.ToString();
                                        if (!string.IsNullOrEmpty(version))
                                        {
                                            LoadedNamespaces.Add(Namespaces.OpcUa, new Tuple<string, string>(requiredModel.Version, version));
                                        }

                                        break;
                                    }
                                }
                            }
                        }

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

                    LoadedNamespaces[nodeManager.NamespaceUris.Last()] = new Tuple<string, string>(requiredModel.Version, dependentNodeset.Version);
                }
            }
        }

        // Guards changes of _current / _sessionNodesetIdentifier. Held only for synchronous field
        // updates, never across an await.
        private readonly object _sessionSwitchGate = new();

        // Makes a borrowed pair this client's current one, atomically with its nodeset tag.
        private void InstallSession(SharedSession shared, string nodesetIdentifier)
        {
            lock (_sessionSwitchGate)
            {
                _current = shared;
                _sessionNodesetIdentifier = nodesetIdentifier;
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
                nodeToBrowse.BrowseDirection,
                nodeToBrowse.ReferenceTypeId,
                nodeToBrowse.IncludeSubtypes,
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
            SharedSession pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
            if (pair == null)
            {
                return string.Empty;
            }

            Opc.Ua.Client.ISession session = pair.Session;

            // read the variable node from the OPC UA server
            Node node = await session.ReadNodeAsync(ExpandedNodeId.ToNodeId(new ExpandedNodeId(nodeId), session.NamespaceUris)).ConfigureAwait(false);
            if (node?.NodeClass == NodeClass.DataType)
            {
                // return complex type definition
                DataTypeNode dataTypeNode = (DataTypeNode)node;
                return new { Namespaces = session.NamespaceUris.ToArray(), dataTypeNode.DataTypeDefinition };
            }
            else
            {
                // return references
                ReferenceDescriptionCollection references = await Browse(session, new BrowseDescription {
                    NodeId = ExpandedNodeId.ToNodeId(new ExpandedNodeId(nodeId), session.NamespaceUris),
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = null,
                    IncludeSubtypes = true,
                    NodeClassMask = 0,
                    ResultMask = (uint)BrowseResultMask.All
                }).ConfigureAwait(false);
                return new { Namespaces = session.NamespaceUris.ToArray(), references };
            }
        }

        public async Task<string> VariableRead(string userId, string nodesetIdentifier, string nodeId)
        {
            string value = string.Empty;

            try
            {
                ReadValueIdCollection nodesToRead = new();

                SharedSession pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
                {
                    return string.Empty;
                }

                Opc.Ua.Client.ISession session = pair.Session;

                // read the variable node from the OPC UA server
                VariableNode node = (VariableNode)await session.ReadNodeAsync(ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris)).ConfigureAwait(false);

                ReadValueId valueId = new();
                valueId.NodeId = ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris);
                valueId.AttributeId = Attributes.Value;
                valueId.IndexRange = null;
                valueId.DataEncoding = null;
                nodesToRead.Add(valueId);

                // Load the data type's complex type once per session. ComplexTypeSystem emits a new
                // dynamic assembly for every type it builds, so creating one per read regenerated
                // the same types on every value read and made each read slow.
                await EnsureComplexTypeLoadedAsync(session, node.DataType).ConfigureAwait(false);

                ReadResponse response = await session.ReadAsync(null, 0, TimestampsToReturn.Both, nodesToRead, CancellationToken.None).ConfigureAwait(false);

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

        private sealed class SessionTypeCache
        {
            public SessionTypeCache(Opc.Ua.Client.ISession session) => TypeSystem = new ComplexTypeSystem(session);

            public ComplexTypeSystem TypeSystem { get; }

            public ConcurrentDictionary<ExpandedNodeId, bool> Loaded { get; } = new();

            public SemaphoreSlim Gate { get; } = new(1, 1);
        }

        // Keyed weakly by session so a closed session's type system is collected with it.
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Opc.Ua.Client.ISession, SessionTypeCache> _typeCaches = new();

        private static async Task EnsureComplexTypeLoadedAsync(Opc.Ua.Client.ISession session, ExpandedNodeId dataType)
        {
            if (session == null || NodeId.IsNull(dataType))
            {
                return;
            }

            SessionTypeCache cache = _typeCaches.GetValue(session, s => new SessionTypeCache(s));
            if (cache.Loaded.ContainsKey(dataType))
            {
                return;
            }

            await cache.Gate.WaitAsync().ConfigureAwait(false);
            try
            {
                if (!cache.Loaded.ContainsKey(dataType))
                {
                    await cache.TypeSystem.LoadTypeAsync(dataType).ConfigureAwait(false);
                    cache.Loaded[dataType] = true;
                }
            }
            finally
            {
                cache.Gate.Release();
            }
        }

        /// <summary>
        /// Returns the fields of an enumeration DataType node, or null if the node is not an enumeration.
        /// The DataTypeDefinition attribute is used first; the EnumStrings/EnumValues properties are used as fallback.
        /// </summary>
        public async Task<List<EnumFieldInfo>> ReadEnumFields(string userId, string nodesetIdentifier, string nodeId)
        {
            try
            {
                SharedSession pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
                {
                    return null;
                }

                Opc.Ua.Client.ISession session = pair.Session;
                NodeId id = ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris);

                ReadValueIdCollection nodesToRead = new() {
                    new ReadValueId { NodeId = id, AttributeId = Attributes.NodeClass },
                    new ReadValueId { NodeId = id, AttributeId = Attributes.DataTypeDefinition }
                };

                ReadResponse response = await session.ReadAsync(null, 0, TimestampsToReturn.Neither, nodesToRead, CancellationToken.None).ConfigureAwait(false);
                ClientBase.ValidateResponse(response.Results, nodesToRead);

                if (response.Results.Count < 2 || response.Results[0].Value is not int nodeClass || (NodeClass)nodeClass != NodeClass.DataType)
                {
                    return null;
                }

                if (ExtensionObject.ToEncodeable(response.Results[1].Value as ExtensionObject) is EnumDefinition enumDefinition && enumDefinition.Fields != null && enumDefinition.Fields.Count > 0)
                {
                    return enumDefinition.Fields.Select(f => new EnumFieldInfo {
                        Name = f.Name ?? string.Empty,
                        Value = f.Value,
                        DisplayName = f.DisplayName?.Text ?? string.Empty,
                        Description = f.Description?.Text ?? string.Empty
                    }).ToList();
                }

                return await ReadEnumFieldsFromProperties(session, id).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReadEnumFields: " + ex.Message);
                return null;
            }
        }

        private async Task<List<EnumFieldInfo>> ReadEnumFieldsFromProperties(Opc.Ua.Client.ISession session, NodeId dataTypeId)
        {
            BrowseDescription nodeToBrowse = new() {
                NodeId = dataTypeId,
                BrowseDirection = BrowseDirection.Forward,
                ReferenceTypeId = ReferenceTypeIds.HasProperty,
                IncludeSubtypes = true,
                NodeClassMask = (uint)NodeClass.Variable,
                ResultMask = (uint)BrowseResultMask.All
            };

            ReferenceDescriptionCollection references = await Browse(session, nodeToBrowse).ConfigureAwait(false);
            if (references == null)
            {
                return null;
            }

            foreach (ReferenceDescription reference in references)
            {
                string name = reference.BrowseName?.Name;
                if (name != BrowseNames.EnumStrings && name != BrowseNames.EnumValues)
                {
                    continue;
                }

                DataValue dataValue = await session.ReadValueAsync(ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris)).ConfigureAwait(false);

                if (dataValue?.Value is LocalizedText[] enumStrings)
                {
                    return enumStrings.Select((s, i) => new EnumFieldInfo {
                        Name = s?.Text ?? string.Empty,
                        Value = i,
                        DisplayName = s?.Text ?? string.Empty
                    }).ToList();
                }

                if (dataValue?.Value is ExtensionObject[] enumValues)
                {
                    List<EnumFieldInfo> fields = new();
                    foreach (ExtensionObject extensionObject in enumValues)
                    {
                        if (ExtensionObject.ToEncodeable(extensionObject) is EnumValueType enumValue)
                        {
                            fields.Add(new EnumFieldInfo {
                                Name = enumValue.DisplayName?.Text ?? string.Empty,
                                Value = enumValue.Value,
                                DisplayName = enumValue.DisplayName?.Text ?? string.Empty,
                                Description = enumValue.Description?.Text ?? string.Empty
                            });
                        }
                    }

                    if (fields.Count > 0)
                    {
                        return fields;
                    }
                }
            }

            return null;
        }

        public async Task<bool> VariableWrite(string userId, string nodesetIdentifier, string nodeId, string payload)
        {
            try
            {
                SharedSession pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
                if (pair == null)
                {
                    return false;
                }

                Opc.Ua.Client.ISession session = pair.Session;
                NodeId nodeID = ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris);

                WriteValue nodeToWrite = new() {
                    NodeId = nodeID,
                    AttributeId = Attributes.Value,
                    Value = new DataValue(new Variant(await ConvertPayloadToNodeDataType(session, nodeID, payload).ConfigureAwait(false)))
                };

                WriteValueCollection nodesToWrite = new() {
                    nodeToWrite
                };

                WriteResponse response = await session.WriteAsync(null, nodesToWrite, CancellationToken.None).ConfigureAwait(false);

                ClientBase.ValidateResponse(response.Results, nodesToWrite);
                ClientBase.ValidateDiagnosticInfos(response.DiagnosticInfos, nodesToWrite);

                if (StatusCode.IsBad(response.Results[0]))
                {
                    throw ServiceResultException.Create(response.Results[0], 0, response.DiagnosticInfos, response.ResponseHeader.StringTable);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing OPC UA node failed: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Checks whether the value of the given node can be edited from the browser UI.
        /// Returns null when the node is writable, otherwise a human-readable reason.
        /// </summary>
        public async Task<string> GetVariableWriteBlocker(string userId, string nodesetIdentifier, string nodeId)
        {
            SharedSession pair;
            try
            {
                pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "The node's write permissions could not be determined: " + ex.Message;
            }

            if (pair == null)
            {
                return "No OPC UA session is available for this nodeset.";
            }

            return await GetVariableWriteBlockerAsync(pair.Session, nodeId).ConfigureAwait(false);
        }

        private static async Task<string> GetVariableWriteBlockerAsync(Opc.Ua.Client.ISession session, string nodeId)
        {
            try
            {
                Node node = await session.ReadNodeAsync(ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris)).ConfigureAwait(false);
                if (node == null)
                {
                    return "The node could not be read from the nodeset.";
                }

                if (node is not VariableNode variable)
                {
                    return $"Only Variable nodes have a value that can be edited (this node is a {node.NodeClass}).";
                }

                // Namespace 0 nodes (e.g. Server/ServerStatus) belong to the embedded OPC UA server, not
                // to the uploaded nodeset, and are skipped by BrowseVariableNodesResursivelyAsync when
                // node values are saved - so an edit could never be persisted.
                if (node.NodeId.NamespaceIndex == 0)
                {
                    return "This node belongs to the OPC UA base namespace and is maintained by the server itself; it is not part of this nodeset and is not included when node values are saved.";
                }

                if ((variable.AccessLevel & AccessLevels.CurrentWrite) == 0)
                {
                    return "The node's AccessLevel does not include CurrentWrite, so its value is read-only.";
                }

                if ((variable.UserAccessLevel & AccessLevels.CurrentWrite) == 0)
                {
                    return "The node's UserAccessLevel does not include CurrentWrite, so its value is read-only for the current user.";
                }

                if (variable.ValueRank != ValueRanks.Scalar && variable.ValueRank != ValueRanks.ScalarOrOneDimension && variable.ValueRank != ValueRanks.Any)
                {
                    return "Array values cannot be edited as a single text value.";
                }

                BuiltInType builtInType = TypeInfo.GetBuiltInType(variable.DataType, session.TypeTree);
                switch (builtInType)
                {
                    case BuiltInType.Null:
                    case BuiltInType.ExtensionObject:
                    case BuiltInType.Variant:
                    case BuiltInType.DataValue:
                    case BuiltInType.DiagnosticInfo:
                        return $"The node's data type ({variable.DataType}) is a structured or abstract type that cannot be edited as text.";
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                return "The node's write permissions could not be determined: " + ex.Message;
            }
        }

        /// <summary>
        /// Validates that a value could be stored for the given node in a user value set without
        /// writing to the shared server: the node must be editable (see <see cref="GetVariableWriteBlocker"/>)
        /// and the text must convert to the node's data type. Returns null when valid, otherwise a reason.
        /// </summary>
        public async Task<string> ValidateValueForNode(string userId, string nodesetIdentifier, string nodeId, string value)
        {
            // One session for both the permission check and the conversion check.
            SharedSession pair;
            try
            {
                pair = await AcquireSessionAsync(userId, nodesetIdentifier).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return "The node's write permissions could not be determined: " + ex.Message;
            }

            if (pair == null)
            {
                return "No OPC UA session is available for this nodeset.";
            }

            Opc.Ua.Client.ISession session = pair.Session;
            string blocker = await GetVariableWriteBlockerAsync(session, nodeId).ConfigureAwait(false);
            if (blocker != null)
            {
                return blocker;
            }

            try
            {
                Node node = await session.ReadNodeAsync(ExpandedNodeId.ToNodeId(nodeId, session.NamespaceUris)).ConfigureAwait(false);
                if (node is VariableNode variable)
                {
                    BuiltInType builtInType = TypeInfo.GetBuiltInType(variable.DataType, session.TypeTree);
                    if (builtInType != BuiltInType.Null && builtInType != BuiltInType.String)
                    {
                        TypeInfo.Cast(value, builtInType);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                return $"The value '{value}' cannot be converted to the node's data type: {ex.Message}";
            }
        }

        /// <summary>
        /// Returns the canonical node values of the nodeset with the given user value set applied on
        /// top, so a private set can be exported in the same shape as the owner's saved values.
        /// </summary>
        public async Task<Dictionary<string, string>> ExportValuesAsync(string userId, string nodesetIdentifier, IReadOnlyDictionary<string, string> overlay)
        {
            Dictionary<string, string> values = await BrowseVariableNodesResursivelyAsync(userId, nodesetIdentifier, null).ConfigureAwait(false)
                ?? new Dictionary<string, string>();

            if (overlay != null)
            {
                foreach (KeyValuePair<string, string> kv in overlay)
                {
                    values[kv.Key] = kv.Value;
                }
            }

            return values;
        }

        // The browser UI always submits a string, but the server rejects writes whose value does
        // not match the variable's data type (BadTypeMismatch). Cast to the target built-in type
        // when possible and fall back to the raw string otherwise.
        private static async Task<object> ConvertPayloadToNodeDataType(Opc.Ua.Client.ISession session, NodeId nodeID, string payload)
        {
            try
            {
                Node node = await session.ReadNodeAsync(nodeID).ConfigureAwait(false);
                if (node is VariableNode variable)
                {
                    BuiltInType builtInType = TypeInfo.GetBuiltInType(variable.DataType, session.TypeTree);
                    if (builtInType != BuiltInType.Null && builtInType != BuiltInType.String)
                    {
                        return TypeInfo.Cast(payload, builtInType);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Converting payload to node data type failed, writing as string: " + ex.Message);
            }

            return payload;
        }

        // Returns the live pair for a nodeset, borrowing or creating it as needed, or null if no
        // session could be established. Callers use the returned pair for the whole operation and
        // never re-read _current after an await, so a concurrent selection of another nodeset on
        // this client cannot redirect an operation that is already running.
        private async Task<SharedSession> AcquireSessionAsync(string userId, string nodesetIdentifier)
        {
            if (string.IsNullOrEmpty(nodesetIdentifier))
            {
                Console.WriteLine("Failed to validate session: No nodeset identifier specified.");
                return null;
            }

            // Reuse the current pair if it is for this nodeset and still connected.
            lock (_sessionSwitchGate)
            {
                if (_current != null && string.Equals(_sessionNodesetIdentifier, nodesetIdentifier, StringComparison.Ordinal) && IsConnected(_current))
                {
                    return _current;
                }
            }

            // Otherwise this client may already borrow a live pair for the nodeset, or one may be
            // cached; both are found by TryBorrowLiveSession.
            {
                EndpointDescription selectedEndpoint = null;

                if (TryBorrowLiveSession(nodesetIdentifier, out SharedSession value))
                {
                    Console.WriteLine("Re-using existing OPC UA server and session for nodeset " + nodesetIdentifier);
                    InstallSession(value, nodesetIdentifier);
                    return value;
                }
                else
                {
                    Console.WriteLine("Starting new OPC UA server and session for nodeset " + nodesetIdentifier);

                    await _lock.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        // Another caller on this instance may have started the server and created the
                        // session while we waited for the lock (e.g. tree expand and state restore run
                        // concurrently after a page load). Starting a second server here would reload
                        // the dependent nodesets into LoadedNamespaces a second time and fail, and the
                        // resulting DisposeSession would tear down the good session as well.
                        if (TryBorrowLiveSession(nodesetIdentifier, out SharedSession existing))
                        {
                            Console.WriteLine("Re-using OPC UA session created concurrently for nodeset " + nodesetIdentifier);
                            InstallSession(existing, nodesetIdentifier);
                            return existing;
                        }

                        int maxRetry = 5000;
                        while (selectedEndpoint == null)
                        {
                            if (maxRetry < 1)
                            {
                                Console.WriteLine("Failed to start OPC UA server: Max retries reached!");
                                return null;
                            }

                            try
                            {
                                Console.WriteLine("Starting OPC UA server on port " + _port);

                                _server = new SimpleServer(_app, _port);
                                _orphanServers.Add(_server);

                                await _app.StartAsync(_server).ConfigureAwait(false);

                                selectedEndpoint = await CoreClientUtils.SelectEndpointAsync(_app.ApplicationConfiguration, "opc.tcp://localhost:" + _port, true, _telemetry).ConfigureAwait(false);
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

                            if (!LoadedNamespaces.ContainsKey(nodeManager.NamespaceUris.Last()))
                            {
                                NodeSetModel nodeSetMeta = await _database.GetNodeSets(userId, nodesetIdentifier).FirstOrDefaultAsync().ConfigureAwait(false);
                                if (nodeSetMeta != null)
                                {
                                    LoadedNamespaces.Add(nodeManager.NamespaceUris.Last(), new Tuple<string, string>(nodeSetMeta.Version, nodeSetMeta.Version));
                                }
                                else
                                {
                                    Console.WriteLine($"Nodeset metadata for {nodesetIdentifier} not found in database.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Nodeset {nodesetIdentifier} not found in database.");
                            return null;
                        }

                        ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(_app.ApplicationConfiguration));
                        Opc.Ua.Client.ISession newSession = await new DefaultSessionFactory(_telemetry).CreateAsync(
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
                            return null;
                        }

                        await newSession.FetchNamespaceTablesAsync().ConfigureAwait(false);

                        // The pair now owns the session's keepalive and reconnect.
                        var shared = new SharedSession(newSession, _server, _telemetry) { Borrowers = 1 };
                        _orphanServers.Remove(_server);

                        // Register in the cache and as this client's borrow in one step, releasing any
                        // earlier pair this client still held for the nodeset (e.g. a disconnected one
                        // that was evicted); otherwise its count could never reach zero.
                        SharedSession previous;
                        lock (s_poolGate)
                        {
                            previous = TakePreviousBorrow(nodesetIdentifier);
                            _sessions[nodesetIdentifier] = shared;
                            _borrowed[nodesetIdentifier] = shared;
                        }

                        if (previous != null)
                        {
                            await previous.CloseAsync().ConfigureAwait(false);
                        }

                        InstallSession(shared, nodesetIdentifier);

                        Console.WriteLine("OPC UA server started and session created for nodeset " + nodesetIdentifier);
                        return shared;
                    }
                    finally
                    {
                        _lock.Release();
                    }
                }
            }
        }

        private static bool IsConnected(SharedSession pair)
        {
            try
            {
                return pair.Session.Connected;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Copies an existing nodeset under a new title, republishing it with the current date.
        /// </summary>
        /// <remarks>
        /// Returns an <see cref="UploadResult"/> rather than a message so the caller can tell a
        /// failure that changed nothing from one that left the copied nodeset stored. The copy may
        /// carry a DPP, so the caller is also responsible for auditing the operation.
        /// </remarks>
        public async Task<UploadResult> CopyNodeset(string userId, string nodesetIdentifier, string name)
        {
            try
            {
                DbFiles file = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
                if (file == null)
                {
                    return UploadResult.Failed($"Nodeset {nodesetIdentifier} was not found.");
                }

                UANameSpace metadata = await _database.RetrieveAllMetadataAsync(userId, uint.Parse(nodesetIdentifier, CultureInfo.InvariantCulture)).ConfigureAwait(false);
                if (metadata?.Nodeset == null)
                {
                    return UploadResult.Failed($"Metadata for nodeset {nodesetIdentifier} was not found.");
                }

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

                return await _database.UploadNamespaceAndNodesetWithResultAsync(userId, metadata, file.Values, false).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CopyNodeset: " + ex.Message);

                // Everything that can throw here runs before the upload is invoked, so nothing was
                // written. The upload itself reports its own commit state and does not throw.
                return UploadResult.Failed(ex.Message);
            }
        }

        public async Task<Dictionary<string, DbFiles>> GetAllLoadedNodesetFilesAsync(string userId)
        {
            var nodesetFiles = new Dictionary<string, DbFiles>();

            await _lock.WaitAsync().ConfigureAwait(false);
            KeyValuePair<string, Tuple<string, string>>[] loadedNamespacesSnapshot;
            try
            {
                loadedNamespacesSnapshot = LoadedNamespaces.ToArray();
            }
            finally
            {
                _lock.Release();
            }

            foreach (var loadedNamespace in loadedNamespacesSnapshot)
            {
                try
                {
                    // Get nodeset metadata by namespace URI
                    var matchingNodeSets = await _database.GetNodeSets(userId, null, loadedNamespace.Key).ToListAsync().ConfigureAwait(false);

                    if (matchingNodeSets != null && matchingNodeSets.Count > 0)
                    {
                        // Get the nodeset that matches the loaded version
                        var nodeSet = matchingNodeSets.FirstOrDefault(ns => ns.Version == loadedNamespace.Value.Item2) ?? matchingNodeSets.OrderByDescending(ns => ns.PublicationDate ?? DateTime.MinValue).ThenByDescending(ns => ns.Version).First();

                        // Download the file
                        DbFiles file = await _storage.DownloadFileAsync(nodeSet.Identifier).ConfigureAwait(false);
                        if (file != null)
                        {
                            // Use namespace + version as key to ensure uniqueness
                            string fileKey = $"{loadedNamespace.Key}_{loadedNamespace.Value.Item2}";
                            nodesetFiles[fileKey] = file;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving nodeset for {loadedNamespace.Key}: {ex.Message}");
                }
            }

            return nodesetFiles;
        }

        // Takes a reference on the cached pair for a nodeset if it is still live, and records it as
        // borrowed by this client. A dead pair stops being handed out; clients that already borrow it
        // keep their reference and release it themselves, and the last one closes it.
        private bool TryBorrowLiveSession(string nodesetIdentifier, out SharedSession shared)
        {
            shared = null;
            SharedSession previous = null;

            lock (s_poolGate)
            {
                if (!_sessions.TryGetValue(nodesetIdentifier, out SharedSession cached) || cached == null)
                {
                    return false;
                }

                bool live;
                try
                {
                    live = cached.Session.Connected;
                }
                catch (ObjectDisposedException)
                {
                    live = false;
                }

                if (!live)
                {
                    _sessions.TryRemove(new KeyValuePair<string, SharedSession>(nodesetIdentifier, cached));
                    return false;
                }

                // A client holds at most one reference per nodeset; drop any older one first.
                if (!_borrowed.TryGetValue(nodesetIdentifier, out SharedSession mine) || !ReferenceEquals(mine, cached))
                {
                    previous = TakePreviousBorrow(nodesetIdentifier);
                    cached.Borrowers++;
                    _borrowed[nodesetIdentifier] = cached;
                }

                shared = cached;
            }

            if (previous != null)
            {
                _ = previous.CloseAsync();
            }

            return true;
        }

        // Removes this client's borrow for a nodeset and decrements the pair's count. Returns the pair
        // if this was its last borrower, so the caller closes it outside the lock. Call under s_poolGate.
        private SharedSession TakePreviousBorrow(string nodesetIdentifier)
        {
            if (!_borrowed.Remove(nodesetIdentifier, out SharedSession shared))
            {
                return null;
            }

            shared.Borrowers--;
            if (shared.Borrowers > 0)
            {
                return null;
            }

            _sessions.TryRemove(new KeyValuePair<string, SharedSession>(nodesetIdentifier, shared));
            return shared;
        }

        // Drops this client's reference to a nodeset's pair. Only the last borrower closes the
        // session and stops its server; until then the pair stays usable by the others.
        private async Task ReleaseAsync(string nodesetIdentifier)
        {
            SharedSession toClose;
            lock (s_poolGate)
            {
                toClose = TakePreviousBorrow(nodesetIdentifier);
            }

            if (toClose != null)
            {
                await toClose.CloseAsync().ConfigureAwait(false);
            }
        }

        private static async Task StopServerAsync(SimpleServer server)
        {
            if (server == null)
            {
                return;
            }

            try
            {
                await server.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to stop OPC UA server: " + ex.Message);
            }
        }

        // Called after an operation on a pair failed. Acts only on that pair - the one the operation
        // actually used - never on whatever this client has selected since. The session is shared, so
        // it is not closed outright: it stops being handed out, and this client releases its
        // reference. Other borrowers keep theirs; the last to release it closes it.
        private async Task DisposeSession(SharedSession pair, string nodesetIdentifier)
        {
            if (pair == null || nodesetIdentifier == null)
            {
                return;
            }

            lock (_sessionSwitchGate)
            {
                if (ReferenceEquals(_current, pair))
                {
                    _current = null;
                    _sessionNodesetIdentifier = null;
                }
            }

            SharedSession toClose;
            lock (s_poolGate)
            {
                // Check identity and release in one step: once this lock is released another
                // operation on this client could replace the borrow for the nodeset, and releasing
                // by nodeset afterwards would drop that replacement instead of the failed pair.
                if (!_borrowed.TryGetValue(nodesetIdentifier, out SharedSession held) || !ReferenceEquals(held, pair))
                {
                    return;
                }

                _sessions.TryRemove(new KeyValuePair<string, SharedSession>(nodesetIdentifier, pair));
                toClose = TakePreviousBorrow(nodesetIdentifier);
            }

            // Only the captured pair is closed, and only if this client was its last borrower.
            if (toClose != null)
            {
                await toClose.CloseAsync().ConfigureAwait(false);
            }
        }

        public async ValueTask DisposeAsync()
        {
            lock (_sessionSwitchGate)
            {
                _current = null;
            }

            // Release every pair this client borrows; each closes only if this was its last borrower.
            // Keepalive and reconnect belong to the pair, so they keep running for the others.
            List<string> borrowed;
            lock (s_poolGate)
            {
                borrowed = _borrowed.Keys.ToList();
            }

            foreach (string nodeset in borrowed)
            {
                await ReleaseAsync(nodeset).ConfigureAwait(false);
            }

            foreach (SimpleServer server in _orphanServers)
            {
                await StopServerAsync(server).ConfigureAwait(false);
            }

            _orphanServers.Clear();
            _server = null;
        }
    }
}
