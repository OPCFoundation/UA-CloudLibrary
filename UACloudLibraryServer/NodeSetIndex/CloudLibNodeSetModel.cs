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
using System.Threading.Tasks;
using CESMII.OpcUa.NodeSetModel;
using CESMII.OpcUa.NodeSetModel.EF;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public class CloudLibNodeSetModel : NodeSetModel
    {
        public virtual NamespaceMetaDataModel Metadata { get; set; }
        public ValidationStatus ValidationStatus { get; set; }
        public string ValidationStatusInfo { get; set; }
        public TimeSpan ValidationElapsedTime { get; set; }
        public DateTime? ValidationFinishedTime { get; set; }
        public string[] ValidationErrors { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        internal static async Task<CloudLibNodeSetModel> FromModelAsync(ModelTableEntry model, AppDbContext dbContext)
        {
            var nodeSetModel = new CloudLibNodeSetModel();
            nodeSetModel.ModelUri = model.ModelUri;
            nodeSetModel.Version = model.Version;
            nodeSetModel.PublicationDate = model.PublicationDateSpecified
                ? model.PublicationDate
                : DateTime.MinValue; // Upload without a publication date is disallowed, but there are 2 nodesets already in the cloudlibrary 

            if (model.RequiredModel != null)
            {
                foreach (var requiredModel in model.RequiredModel)
                {
                    var existingNodeSet = await DbOpcUaContext.GetMatchingOrHigherNodeSetAsync(dbContext, requiredModel.ModelUri, requiredModel.PublicationDateSpecified ? requiredModel.PublicationDate : null, requiredModel.Version).ConfigureAwait(false);
                    var requiredModelInfo = new RequiredModelInfo {
                        ModelUri = requiredModel.ModelUri,
                        PublicationDate = requiredModel.PublicationDateSpecified ? requiredModel.PublicationDate : null,
                        Version = requiredModel.Version,
                        AvailableModel = existingNodeSet,
                    };
                    nodeSetModel.RequiredModels.Add(requiredModelInfo);
                }
            }
            return nodeSetModel;
        }
    }
    public enum ValidationStatus
    {
        Parsed,
        Indexed,
        Error,
    }
}
