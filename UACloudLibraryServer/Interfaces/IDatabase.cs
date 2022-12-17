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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using CESMII.OpcUa.NodeSetModel;
    using Opc.Ua.Cloud.Library.DbContextModels;
    using Opc.Ua.Cloud.Library.Models;
    using Opc.Ua.Export;

    public interface IDatabase
    {
        UANodesetResult[] FindNodesets(string[] keywords, int? offset, int? limit);
        IQueryable<CloudLibNodeSetModel> GetNodeSets(string identifier = null, string nodeSetUrl = null, DateTime? publicationDate = null, string[] keywords = null);

        Task<string> AddMetaDataAsync(UANameSpace uaNamespace, UANodeSet nodeSet, uint legacyNodesetHashCode, string userId);

        Task<uint> IncrementDownloadCountAsync(uint nodesetId);

        Task<bool> DeleteAllRecordsForNodesetAsync(uint nodesetId);

        Task<UANameSpace> RetrieveAllMetadataAsync(uint nodesetId);

        string[] GetAllNamespacesAndNodesets();

        string[] GetAllNamesAndNodesets();

        IQueryable<ObjectTypeModel> GetObjectTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<VariableTypeModel> GetVariableTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<DataTypeModel> GetDataTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<PropertyModel> GetProperties(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<DataVariableModel> GetDataVariables(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<ReferenceTypeModel> GetReferenceTypes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<InterfaceModel> GetInterfaces(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<ObjectModel> GetObjects(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);
        IQueryable<NodeModel> GetAllNodes(string nodeSetUrl = null, DateTime? publicationDate = null, string nodeId = null);

        IQueryable<CategoryModel> GetCategories();
        IQueryable<OrganisationModel> GetOrganisations();

        IQueryable<NamespaceMetaDataModel> GetNamespaces();
        int GetNamespaceTotalCount();

        Task<UANameSpace> ApproveNamespaceAsync(string identifier, ApprovalStatus status, string approvalInformation);
        IQueryable<CloudLibNodeSetModel> GetNodeSetsPendingApproval();

#if !NOLEGACY
        IQueryable<MetadataModel> GetMetadataModel(); // CODE REVIEW: is this still required? Moving to legacy section for now
        IQueryable<DatatypeModel> GetDataType();
        IQueryable<QueryModel.NodeSetGraphQLLegacy> GetNodeSet();
        IQueryable<ObjecttypeModel> GetObjectType();
        IQueryable<ReferencetypeModel> GetReferenceType();
        IQueryable<VariabletypeModel> GetVariableType();
#endif // NOLEGACY
    }
    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected,
    }

}
