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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Cloud.Library;
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
        private Session _session;
        private SessionReconnectHandler _reconnectHandler;

        private static uint _port = 5000;

        public UAClient(ApplicationInstance app, DbFileStorage storage, CloudLibDataProvider database)
        {
            _app = app;
            _storage = storage;
            _session = null;
            _reconnectHandler = null;
            _database = database;
        }

        public async Task<List<NodesetViewerNode>> GetChildren(string nodesetIdentifier, string nodeId)
        {
            List<NodesetViewerNode> nodes = null;
            ReferenceDescriptionCollection references = null;

            try
            {
                if (_session == null || !_session.Connected)
                {
                    _session = await CreateSessionAsync(nodesetIdentifier).ConfigureAwait(false);
                }

                if (_session == null || !_session.Connected)
                {
                    return null;
                }
                else
                {
                    _session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);
                }

                _session.FetchNamespaceTables();

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
                _session = null;
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

        private async Task<Session> CreateSessionAsync(string nodesetIdentifier)
        {
            _server = new SimpleServer(_app, _port);

            await _server.StartServerAsync().ConfigureAwait(false);

            EndpointDescription selectedEndpoint = CoreClientUtils.SelectEndpoint(_app.ApplicationConfiguration, "opc.tcp://localhost:" + _port, true);

            lock (_app)
            {
                _port++;

                if (_port > 10000)
                {
                    _port = 5000;
                }
            }

            NodesetFileNodeManager nodeManager = (NodesetFileNodeManager)_server.CurrentInstance.NodeManager.NodeManagers[2];

            // first load dependencies
            await LoadDependentNodesetsRecursiveAsync(nodesetIdentifier, nodeManager).ConfigureAwait(false);

            // now load the nodeset itself
            DbFiles nodesetXml = await _storage.DownloadFileAsync(nodesetIdentifier).ConfigureAwait(false);
            nodeManager.AddNamespace(nodesetXml.Blob);
            nodeManager.AddNodesAndValues(nodesetXml.Blob, nodesetXml.Values);

            ConfiguredEndpoint configuredEndpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(_app.ApplicationConfiguration));
            return await Session.Create(
                    _app.ApplicationConfiguration,
                    configuredEndpoint,
                    true,
                    false,
                    string.Empty,
                    30000,
                    new UserIdentity(new AnonymousIdentityToken()),
                    null).ConfigureAwait(false);
        }

        private async Task LoadDependentNodesetsRecursiveAsync(string nodesetIdentifier, NodesetFileNodeManager nodeManager)
        {
            NodeSetModel nodeSetMeta = await _database.GetNodeSets(nodesetIdentifier).FirstOrDefaultAsync().ConfigureAwait(false);
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
                    List<NodeSetModel> matchingNodeSets = await _database.NodeSets.Where(nsm => nsm.ModelUri == requiredModel.ModelUri).ToListAsync().ConfigureAwait(false);
                    if (matchingNodeSets == null || matchingNodeSets.Count == 0)
                    {
                        Console.WriteLine($"Required model {requiredModel.ModelUri} for {nodesetIdentifier} not found in database.");
                        MissingNamespaces.Add(requiredModel.ModelUri);
                        continue;
                    }

                    NodeSetModel dependentNodeset = NodeModelUtils.GetMatchingOrHigherNodeSet(matchingNodeSets, requiredModel.PublicationDate, requiredModel.Version);

                    await LoadDependentNodesetsRecursiveAsync(dependentNodeset.Identifier, nodeManager).ConfigureAwait(false);

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

            _session = (Session)_reconnectHandler.Session;
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

        private ReferenceDescriptionCollection Browse(Session session, BrowseDescription nodeToBrowse)
        {
            ReferenceDescriptionCollection references = new ReferenceDescriptionCollection();

            BrowseDescriptionCollection nodesToBrowse = new BrowseDescriptionCollection
            {
                nodeToBrowse
            };

            try
            {
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

                    ByteStringCollection continuationPoints = new ByteStringCollection
                {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse: " + ex.Message);
                _session = null;
            }

            return references;
        }

        public async Task<IServiceResponse> Browse(string nodesetIdentifier, BrowseRequest request)
        {
            try
            {
                if (_session == null || !_session.Connected)
                {
                    _session = await CreateSessionAsync(nodesetIdentifier).ConfigureAwait(false);
                }

                if (_session == null || !_session.Connected)
                {
                    return null;
                }
                else
                {
                    _session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);
                }

                _session.FetchNamespaceTables();

                ResponseHeader responseHeader = _session.Browse(
                    request.RequestHeader,
                    request.View,
                    request.RequestedMaxReferencesPerNode,
                    request.NodesToBrowse,
                    out BrowseResultCollection results,
                    out DiagnosticInfoCollection diagnosticInfos);

                BrowseResponse response = new() {
                    ResponseHeader = responseHeader,
                    Results = results,
                    DiagnosticInfos = diagnosticInfos
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Browse: " + ex.Message);
                _session = null;
                return null;
            }
        }

        public async Task<string> VariableRead(string nodesetIdentifier, string nodeId)
        {
            string value = string.Empty;

            try
            {
                DataValueCollection values = null;
                DiagnosticInfoCollection diagnosticInfos = null;
                ReadValueIdCollection nodesToRead = new();

                if (_session == null || !_session.Connected)
                {
                    _session = await CreateSessionAsync(nodesetIdentifier).ConfigureAwait(false);
                }

                if (_session == null || !_session.Connected)
                {
                    return string.Empty;
                }

                _session.FetchNamespaceTables();

                ReadValueId valueId = new();
                valueId.NodeId = ExpandedNodeId.ToNodeId(nodeId, _session.NamespaceUris);
                valueId.AttributeId = Attributes.Value;
                valueId.IndexRange = null;
                valueId.DataEncoding = null;
                nodesToRead.Add(valueId);

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
                _session = null;
            }

            return value;
        }

        public async Task<IServiceResponse> Read(string nodesetIdentifier, ReadRequest request)
        {
            try
            {
                if (_session == null || !_session.Connected)
                {
                    _session = await CreateSessionAsync(nodesetIdentifier).ConfigureAwait(false);
                }

                if (_session == null || !_session.Connected)
                {
                    return null;
                }
                else
                {
                    _session.KeepAlive += new KeepAliveEventHandler(StandardClient_KeepAlive);
                }

                _session.FetchNamespaceTables();

                ResponseHeader responseHeader = _session.Read(
                    request.RequestHeader,
                    request.MaxAge,
                    request.TimestampsToReturn,
                    request.NodesToRead,
                    out DataValueCollection results,
                    out DiagnosticInfoCollection diagnosticInfos);

                for (int ii = 0; ii < request.NodesToRead.Count; ii++)
                {
                    if (request.NodesToRead[ii].AttributeId == 60)
                    {
                        results[ii] = ReadProperties(request, request.NodesToRead[ii]);
                    }
                }

                ReadResponse response = new() {
                    ResponseHeader = responseHeader,
                    Results = results,
                    DiagnosticInfos = diagnosticInfos
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Read: " + ex.Message);
                _session = null;
                return null;
            }
        }

        private DataValue ReadProperties(ReadRequest request, ReadValueId nodeToRead)
        {
            _session.Browse(
                request.RequestHeader,
                null,
                0,
                new BrowseDescriptionCollection([
                    new BrowseDescription() {
                        NodeId = nodeToRead.NodeId,
                        ReferenceTypeId = ReferenceTypeIds.HasProperty,
                        BrowseDirection = BrowseDirection.Forward,
                        IncludeSubtypes = true,
                        NodeClassMask = (uint)NodeClass.Variable,
                        ResultMask = (uint)BrowseResultMask.BrowseName
                    }
                ]),
                out BrowseResultCollection results1,
                out DiagnosticInfoCollection diagnosticInfos1);

            if (results1.Count == 0)
            {
                return new DataValue() { StatusCode = StatusCodes.BadNotFound, ServerTimestamp = DateTime.UtcNow };
            }

            ReadValueIdCollection nodesToRead = new();

            foreach (var node in results1)
            {
                foreach (var reference in node.References)
                {
                    nodesToRead.Add(new ReadValueId() {
                        NodeId = (NodeId)reference.NodeId,
                        AttributeId = Attributes.Value,
                        Handle = reference
                    });
                }
            }

            var responseHeader = _session.Read(
                request.RequestHeader,
                request.MaxAge,
                TimestampsToReturn.Neither,
                nodesToRead,
                out DataValueCollection results,
                out DiagnosticInfoCollection diagnosticInfos);

            using (JsonEncoder encoder = new(ServiceMessageContext.GlobalContext, JsonEncodingType.Compact))
            {
                encoder.SuppressArtifacts = true;

                for (int ii = 0; ii < nodesToRead.Count; ii++)
                {
                    ReferenceDescription reference = nodesToRead[ii].Handle as ReferenceDescription;

                    encoder.WriteRawValue(
                        new FieldMetaData() {
                            Name = reference.BrowseName.Name,
                            BuiltInType = (byte)results[ii].WrappedValue.TypeInfo.BuiltInType,
                            ValueRank = results[ii].WrappedValue.TypeInfo.ValueRank,
                            DataType = DataTypeIds.BaseDataType
                        },
                        results[ii],
                        DataSetFieldContentMask.RawData);
                }

                string json = encoder.CloseAndReturnText();
                JObject jobject = JObject.Parse(json);

                return new DataValue() {
                    WrappedValue = new ExtensionObject(nodeToRead.NodeId, jobject),
                    StatusCode = Opc.Ua.StatusCodes.Good,
                    ServerTimestamp = DateTime.UtcNow
                };
            }
        }

        public void Dispose()
        {
            if (_session != null)
            {
                if (_session.Connected)
                {
                    _session.Close();
                }

                _session.Dispose();
                _session = null;
            }

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
