/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
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

using CESMII.OpcUa.NodeSetModel;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Opc.Ua.Cloud.Library
{
    public class QueryModel
    {
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<NodeSetModel> GetNodeSets([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null)
        {
            IQueryable<NodeSetModel> nodeSets;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
            }
            else
            {
                nodeSets = dbContext.nodeSets.AsQueryable();
            }
            return nodeSets;
        }
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<ObjectTypeModel> GetObjectTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectTypeModel>(dbContext, nsm => nsm.ObjectTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<VariableTypeModel> GetVariableTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<VariableTypeModel>(dbContext, nsm => nsm.VariableTypes, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<DataTypeModel> GetDataTypes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataTypeModel>(dbContext, nsm => nsm.DataTypes, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<PropertyModel> GetProperties([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<PropertyModel>(dbContext, nsm => nsm.Properties, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<DataVariableModel> GetDataVariables([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<DataVariableModel>(dbContext, nsm => nsm.DataVariables, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<InterfaceModel> GetInterfaces([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<InterfaceModel>(dbContext, nsm => nsm.Interfaces, nodeSetUrl, publicationDate, nodeId);
        }

        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<ObjectModel> GetObjects([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            return GetNodeModels<ObjectModel>(dbContext, nsm => nsm.Objects, nodeSetUrl, publicationDate, nodeId);
        }
        [UsePaging(MaxPageSize = 100, DefaultPageSize = 100), UseFiltering, UseSorting]
        public IQueryable<NodeModel> GetAllNodes([Service(ServiceKind.Synchronized)] AppDbContext dbContext, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
        {
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeModel> nodeModels;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeModels = dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl && nm.NodeSet.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeModels = dbContext.nodeModels.AsQueryable().Where(nm => nm.Namespace == nodeSetUrl);
            }
            else
            {
                nodeModels = dbContext.nodeModels.AsQueryable();
            }
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }

            return nodeModels;
        }

        private IQueryable<T> GetNodeModels<T>(AppDbContext dbContext, Expression<Func<NodeSetModel, IEnumerable<T>>> selector, string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null)
            where T : NodeModel
        { 
            if (nodeId != null && nodeSetUrl == null)
            {
                var expandedNodeId = ExpandedNodeId.Parse(nodeId);
                if (expandedNodeId?.NamespaceUri != null)
                {
                    nodeSetUrl = expandedNodeId.NamespaceUri;
                }
            }

            IQueryable<NodeSetModel> nodeSets;
            if (nodeSetUrl != null && publicationDate != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl && nsm.PublicationDate == publicationDate);
            }
            else if (nodeSetUrl != null)
            {
                nodeSets = dbContext.nodeSets.AsQueryable().Where(nsm => nsm.ModelUri == nodeSetUrl);
            }
            else
            {
                nodeSets = dbContext.nodeSets.AsQueryable();
            }
            IQueryable<T> nodeModels = nodeSets.SelectMany(selector);
            if (!string.IsNullOrEmpty(nodeId))
            {
                nodeModels = nodeModels.Where(ot => ot.NodeId == nodeId);
            }
            return nodeModels;
        }
    }
}
