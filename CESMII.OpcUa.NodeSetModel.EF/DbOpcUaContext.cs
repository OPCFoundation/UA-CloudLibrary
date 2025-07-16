/* ========================================================================
 * Copyright (c) 2005-2022 The OPC Foundation, Inc. All rights reserved.
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
using System.Linq;
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using CESMII.OpcUa.NodeSetModel.Opc.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Export;

namespace CESMII.OpcUa.NodeSetModel.EF
{
    public class DbOpcUaContext : DefaultOpcUaContext
    {
        protected DbContext _dbContext;
        protected Func<ModelTableEntry, NodeSetModel> _nodeSetFactory;
        protected List<(string ModelUri, DateTime? PublicationDate)> _namespacesInDb;

        public DbOpcUaContext(DbContext appDbContext, ILogger logger, Func<ModelTableEntry, NodeSetModel> nodeSetFactory = null)
            : base(logger)
        {
            this._dbContext = appDbContext;
            this._nodeSetFactory = nodeSetFactory;
            // Get all namespaces with at least one node: used for avoiding DB lookups
            this._namespacesInDb = _dbContext.Set<NodeModel>().Select(nm => new { nm.NodeSet.ModelUri, nm.NodeSet.PublicationDate }).Distinct().AsEnumerable().Select(n => (n.ModelUri, n.PublicationDate)).ToList();
        }
        public DbOpcUaContext(DbContext appDbContext, SystemContext systemContext, NodeStateCollection importedNodes, Dictionary<string, NodeSetModel> nodesetModels, ILogger logger, Func<ModelTableEntry, NodeSetModel> nodeSetFactory = null)
            : base(systemContext, importedNodes, nodesetModels, logger)
        {
            this._dbContext = appDbContext;
            this._nodeSetFactory = nodeSetFactory;
        }

        protected void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseLazyLoadingProxies(true)
            ;
        public struct NodeSetKey
        {
            public NodeSetKey(string modelUri, string version, DateTime? publicationDate)
            {
                ModelUri = modelUri;
                Version = version;
                PublicationDate = publicationDate;
            }
            #pragma warning disable CA1051
            public string ModelUri;
            public string Version;
            public DateTime? PublicationDate;
            #pragma warning restore CA1051
        };

        public static Dictionary<string,NodeSetKey> DictAvailableNodesets = null;

        public override TNodeModel GetModelForNode<TNodeModel>(string nodeId, bool bSameNamespace = true)
        {
            if (DictAvailableNodesets == null)
            {
                DictAvailableNodesets = new Dictionary<string, NodeSetKey>();
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/", new NodeSetKey("http://opcfoundation.org/UA/", "1.05.02", new DateTime(2022, 10, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://cloudlibtests/testnodeset001/", new NodeSetKey("http://cloudlibtests/testnodeset001/", "1.02.0", new DateTime(2022, 11, 22, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/MDIS", new NodeSetKey("http://opcfoundation.org/UA/MDIS", "1.2", new DateTime(2018, 10, 2, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/ADI/", new NodeSetKey("http://opcfoundation.org/UA/ADI/", "1.01", new DateTime(2013, 7, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Calibrator/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Calibrator/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://fdi-cooperation.com/OPCUA/FDI5/", new NodeSetKey("http://fdi-cooperation.com/OPCUA/FDI5/", "1.1", new DateTime(2017, 7, 13, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Machinery/", new NodeSetKey("http://opcfoundation.org/UA/Machinery/", "1.01.0", new DateTime(2021, 2, 24, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/MTConnect/v2/", new NodeSetKey("http://opcfoundation.org/UA/MTConnect/v2/", "2.00.01", new DateTime(2020, 6, 4, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://cloudlibtests/testnodeset001/", new NodeSetKey("http://cloudlibtests/testnodeset001/", "1.01.2", new DateTime(2021, 5, 19, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IEC61850-7-3", new NodeSetKey("http://opcfoundation.org/UA/IEC61850-7-3", "2", new DateTime(2018, 2, 4, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://cloudlibtests/dependingtestnodeset001/", new NodeSetKey("http://cloudlibtests/dependingtestnodeset001/", "1.02.0", new DateTime(2023, 7, 5, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IEC61850-7-4", new NodeSetKey("http://opcfoundation.org/UA/IEC61850-7-4", "2", new DateTime(2018, 2, 4, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IOLink/IODD/", new NodeSetKey("http://opcfoundation.org/UA/IOLink/IODD/", "1", new DateTime(2018, 11, 30, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/GeneralTypes/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/GeneralTypes/", "1.02", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/TCD/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/TCD/", "1.01", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Pumps/", new NodeSetKey("http://opcfoundation.org/UA/Pumps/", "1.0.0", new DateTime(2021, 4, 18, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/AML/", new NodeSetKey("http://opcfoundation.org/UA/AML/", "1", new DateTime(2016, 2, 21, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Glass/Flat/", new NodeSetKey("http://opcfoundation.org/UA/Glass/Flat/", "1.0.0", new DateTime(2021, 12, 31, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/I4AAS/", new NodeSetKey("http://opcfoundation.org/UA/I4AAS/", "5.0.0", new DateTime(2021, 6, 3, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PADIM/", new NodeSetKey("http://opcfoundation.org/UA/PADIM/", "1.0.2", new DateTime(2021, 7, 20, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/GeneralTypes/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/GeneralTypes/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Robotics/", new NodeSetKey("http://opcfoundation.org/UA/Robotics/", "1.01.2", new DateTime(2021, 5, 19, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("urn:demo:project:one", new NodeSetKey("urn:demo:project:one", "1.01", new DateTime(2013, 7, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/AutoID/", new NodeSetKey("http://opcfoundation.org/UA/AutoID/", "1.01", new DateTime(2020, 6, 18, 6, 52, 6, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IEC61850-6", new NodeSetKey("http://opcfoundation.org/UA/IEC61850-6", "2", new DateTime(2018, 2, 4, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Corrugator/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Corrugator/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Extruder/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Extruder/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/HaulOff/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/HaulOff/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PNEM/", new NodeSetKey("http://opcfoundation.org/UA/PNEM/", "1.0.0", new DateTime(2021, 3, 10, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PROFINET/", new NodeSetKey("http://opcfoundation.org/UA/PROFINET/", "1.0.1", new DateTime(2021, 4, 12, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/GDS/", new NodeSetKey("http://opcfoundation.org/UA/GDS/", "1.04.4", new DateTime(2020, 1, 7, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/DEXPI/", new NodeSetKey("http://opcfoundation.org/UA/DEXPI/", "1.0.0", new DateTime(2021, 9, 9, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/DI/", new NodeSetKey("http://opcfoundation.org/UA/DI/", "1.03.0", new DateTime(2021, 3, 8, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IOLink/", new NodeSetKey("http://opcfoundation.org/UA/IOLink/", "1", new DateTime(2018, 11, 30, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Die/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Die/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Filter/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Filter/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/IMM2MES/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/IMM2MES/", "1.01", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://sercos.org/UA/", new NodeSetKey("http://sercos.org/UA/", "1", new DateTime(2017, 3, 12, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://fdi-cooperation.com/OPCUA/FDI7/", new NodeSetKey("http://fdi-cooperation.com/OPCUA/FDI7/", "", new DateTime(2017, 7, 13, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/CommercialKitchenEquipment/", new NodeSetKey("http://opcfoundation.org/UA/CommercialKitchenEquipment/", "1", new DateTime(2019, 7, 11, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Eumabois/", new NodeSetKey("http://opcfoundation.org/UA/Eumabois/", "0.14", new DateTime(2021, 1, 26, 16, 14, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IA/Examples/", new NodeSetKey("http://opcfoundation.org/UA/IA/Examples/", "1.01.0", new DateTime(2021, 7, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IJT/", new NodeSetKey("http://opcfoundation.org/UA/IJT/", "1.00.0", new DateTime(2021, 9, 29, 2, 23, 2, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/ExtrusionLine/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/ExtrusionLine/", "1.00.01", new DateTime(2020, 11, 8, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/MeltPump/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/MeltPump/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/POWERLINK/", new NodeSetKey("http://opcfoundation.org/UA/POWERLINK/", "1.0.0", new DateTime(2017, 10, 10, 6, 0, 6, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Scales", new NodeSetKey("http://opcfoundation.org/UA/Scales", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Woodworking/", new NodeSetKey("http://opcfoundation.org/UA/Woodworking/", "1", new DateTime(2021, 10, 2, 18, 0, 18, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/MachineVision", new NodeSetKey("http://opcfoundation.org/UA/MachineVision", "1.0.0", new DateTime(2019, 7, 11, 3, 18, 3, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Safety", new NodeSetKey("http://opcfoundation.org/UA/Safety", "1", new DateTime(2019, 10, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PackML/", new NodeSetKey("http://opcfoundation.org/UA/PackML/", "1.01", new DateTime(2020, 10, 8, 4, 8, 4, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://www.OPCFoundation.org/UA/2013/01/ISA95", new NodeSetKey("http://www.OPCFoundation.org/UA/2013/01/ISA95", "1", new DateTime(2013, 11, 5, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/CNC", new NodeSetKey("http://opcfoundation.org/UA/CNC", "1.0.0", new DateTime(2017, 6, 19, 9, 1, 9, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/OPENSCS-SER/", new NodeSetKey("http://opcfoundation.org/UA/OPENSCS-SER/", "1", new DateTime(2019, 2, 3, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/ISA95-JOBCONTROL", new NodeSetKey("http://opcfoundation.org/UA/ISA95-JOBCONTROL", "1.0.0", new DateTime(2021, 3, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Dictionary/IRDI", new NodeSetKey("http://opcfoundation.org/UA/Dictionary/IRDI", "1", new DateTime(2020, 2, 3, 16, 0, 16, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Station/", new NodeSetKey("http://opcfoundation.org/UA/Station/", "1", new DateTime(2017, 4, 19, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/", new NodeSetKey("http://opcfoundation.org/UA/", "1.05.02", new DateTime(2022, 10, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/CAS/", new NodeSetKey("http://opcfoundation.org/UA/CAS/", "1.00.1", new DateTime(2021, 7, 12, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/IA/", new NodeSetKey("http://opcfoundation.org/UA/IA/", "1.01.0", new DateTime(2021, 7, 30, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/MachineTool/", new NodeSetKey("http://opcfoundation.org/UA/MachineTool/", "1.00.0", new DateTime(2020, 9, 24, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Cutter/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Cutter/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Pelletizer/", new NodeSetKey("http://opcfoundation.org/UA/PlasticsRubber/Extrusion/Pelletizer/", "1", new DateTime(2020, 5, 31, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/TMC/", new NodeSetKey("http://opcfoundation.org/UA/TMC/", "1", new DateTime(2017, 10, 11, 5, 0, 5, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://opcfoundation.org/UA/Weihenstephan/", new NodeSetKey("http://opcfoundation.org/UA/Weihenstephan/", "1.00.0", new DateTime(2021, 7, 11, 17, 0, 17, DateTimeKind.Utc)));
                DictAvailableNodesets.Add("http://PLCopen.org/OpcUa/IEC61131-3/", new NodeSetKey("http://PLCopen.org/OpcUa/IEC61131-3/", "1.02", new DateTime(2020, 11, 24, 16, 0, 16, DateTimeKind.Utc)));


            }
            var model = base.GetModelForNode<TNodeModel>(nodeId);
            if (model != null) return model;

            var uaNamespace = NodeModelUtils.GetNamespaceFromNodeId(nodeId);
            NodeModel nodeModelDb;
            if (_nodesetModels.TryGetValue(uaNamespace, out var nodeSet))
            {
                if (!_namespacesInDb.Contains((nodeSet.ModelUri, nodeSet.PublicationDate)))
                {
                    // namespace was not in DB when the context was created: assume it's being imported
                    return null;
                }
                else
                {
                    // Preexisting namespace: find an entity if already in EF cache
                    int retryCount = 0;
                    bool lookedUp = false;
                    do
                    {
                        try
                        {
                            nodeModelDb = _dbContext.Set<NodeModel>().Local.FirstOrDefault(nm => nm.NodeId == nodeId && nm.NodeSet.ModelUri == nodeSet.ModelUri && nm.NodeSet.PublicationDate == nodeSet.PublicationDate);
                            lookedUp = true;
                        }
                        catch (InvalidOperationException)
                        {
                            // re-try in case the NodeSet access caused a database query that modified the local cache
                            nodeModelDb = null;
                        }
                        retryCount++;
                    } while (!lookedUp && retryCount < 100);
                    if (nodeModelDb == null)
                    {
                        string strModelUri = nodeSet.ModelUri;
                        string strVersion = nodeSet.Version;
                        DateTime? publicationDate = nodeSet.PublicationDate;
                        if (DictAvailableNodesets.TryGetValue(nodeSet.ModelUri, out NodeSetKey value))
                        {
                            strVersion = value.Version;
                            publicationDate = value.PublicationDate;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"DbOpcUaContext.GetModelForNode<TNodeModel>: NOT FOUND: {nodeSet.ModelUri}");
                        }



                        // Not in EF cache: assume it's in the database and attach a proxy with just primary key values
                        // This avoids a database lookup for each referenced node (or the need to pre-fetch all nodes in the EF cache)
                        nodeModelDb = _dbContext.CreateProxy<TNodeModel>(nm =>
                        {
                            if (nodeSet.PublicationDate == publicationDate && nodeSet.Version == strVersion)
                            {
                                nm.NodeSet = nodeSet;
                                nm.NodeId = nodeId;
                            }
                            else
                            {
                                NodeSetModel nsmTemp = new NodeSetModel();
                                nsmTemp.ModelUri = strModelUri;
                                nsmTemp.Version = strVersion;
                                nsmTemp.PublicationDate = publicationDate;
                                nsmTemp.Identifier = Guid.NewGuid().ToString();
                                nm.NodeSet = nsmTemp;
                                nm.NodeId = nodeId;
                            }
                        }
                        );
                        // _dbContext.Attach(nodeModelDb);
                        _dbContext.Update(nodeModelDb);
                        if (!bSameNamespace)
                        {
                            var info = _dbContext.ContextId;
                            System.Diagnostics.Debug.WriteLine($"DbOpcUaContext.GetModelForNode<TNodeModel>: bSameNamespace==false CREATING PROXY for {nodeId}. Context id = {info}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"DbOpcUaContext.GetModelForNode<TNodeModel>: Found record for NodeSet = {nodeSet} and nodeId: {nodeId}");
                        //if (!bSameNamespace)
                        //{
                        //    System.Diagnostics.Debug.WriteLine($"DbOpcUaContext.GetModelForNode<TNodeModel>: bSameNamespace==false Found in Cache for {nodeId}");
                        //}

                    }
                }
                nodeModelDb?.NodeSet.AllNodesByNodeId.Add(nodeModelDb.NodeId, nodeModelDb);
            }
            else
            {
                nodeModelDb = _dbContext.Set<NodeModel>().FirstOrDefault(nm => nm.NodeId == nodeId && nm.NodeSet.ModelUri == uaNamespace);
                if (nodeModelDb != null)
                {
                    nodeSet = GetOrAddNodesetModel(new ModelTableEntry { ModelUri = nodeModelDb.NodeSet.ModelUri, PublicationDate = nodeModelDb.NodeSet.PublicationDate ?? DateTime.MinValue, PublicationDateSpecified = nodeModelDb.NodeSet.PublicationDate != null });
                    nodeModelDb?.NodeSet.AllNodesByNodeId.Add(nodeModelDb.NodeId, nodeModelDb);
                }
            }
            if (!(nodeModelDb is TNodeModel))
            {
                _logger.LogWarning($"Nodemodel {nodeModelDb} is of type {nodeModelDb.GetType()} when type {typeof(TNodeModel)} was requested. Returning null.");
            }
            return nodeModelDb as TNodeModel;
        }

        public override NodeSetModel GetOrAddNodesetModel(ModelTableEntry model, bool createNew = true)
        {
            if (!_nodesetModels.TryGetValue(model.ModelUri, out var nodesetModel))
            {
                var existingNodeSet = GetMatchingOrHigherNodeSetAsync(model.ModelUri, model.GetNormalizedPublicationDate(), model.Version).Result;
                if (existingNodeSet != null)
                {
                    _nodesetModels.Add(existingNodeSet.ModelUri, existingNodeSet);
                    nodesetModel = existingNodeSet;
                }
            }
            if (nodesetModel == null && createNew)
            {
                if (_nodeSetFactory == null)
                {
                    nodesetModel = base.GetOrAddNodesetModel(model, createNew);
                    if (nodesetModel.PublicationDate == null)
                    {
                        // Primary Key value can not be null
                        nodesetModel.PublicationDate = DateTime.MinValue;
                    }
                }
                else
                {
                    nodesetModel = _nodeSetFactory.Invoke(model);
                    if (nodesetModel != null)
                    {
                        if (nodesetModel.ModelUri != model.ModelUri)
                        {
                            throw new ArgumentException($"Created mismatching nodeset: expected {model.ModelUri} created {nodesetModel.ModelUri}");
                        }
                        _nodesetModels.Add(nodesetModel.ModelUri, nodesetModel);
                    }
                }
            }
            return nodesetModel;
        }

        public Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(string modelUri, DateTime? publicationDate, string version)
        {
            return GetMatchingOrHigherNodeSetAsync(_dbContext, modelUri, publicationDate, version);
        }
        public static async Task<NodeSetModel> GetMatchingOrHigherNodeSetAsync(DbContext dbContext, string modelUri, DateTime? publicationDate, string version)
        {
            var matchingNodeSets = await dbContext.Set<NodeSetModel>()
                .Where(nsm => nsm.ModelUri == modelUri).ToListAsync();
            return NodeSetVersionUtils.GetMatchingOrHigherNodeSet(matchingNodeSets, publicationDate, version);
        }
    }
}
