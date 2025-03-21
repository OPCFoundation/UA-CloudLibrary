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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Opc.Ua.Cloud.Library.DbContextModels;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    [Authorize]
    public partial class MutationModel
    {
        public class ApprovalInput
        {
            public string Identifier { get; set; }
            public ApprovalStatus Status { get; set; }
            public string ApprovalInformation { get; set; }
            /// <summary>
            /// Set/overwrite these properties upon approval. Null or empty string value deletes the property.
            /// </summary>
            public List<UAProperty> AdditionalProperties { get; set; }
        }

        [Authorize(Policy = "ApprovalPolicy")]
        public async Task<NamespaceMetaDataModel> ApproveNodeSetAsync([Service()] IDatabase db, ApprovalInput input)
        {
            NamespaceMetaDataModel nodeSet = await db.ApproveNamespaceAsync(input.Identifier, input.Status, input.ApprovalInformation, input.AdditionalProperties).ConfigureAwait(false);
            return nodeSet;
        }

    }
    [Authorize]
    public partial class QueryModel
    {
        [UsePaging, UseFiltering, UseSorting]
        public IQueryable<CloudLibNodeSetModel> GetNodeSetsPendingApproval([Service()] IDatabase dp, IResolverContext context)
        {
            IQueryable<CloudLibNodeSetModel> query = dp.GetNodeSetsPendingApproval();

            // Make sure the result is ordered even if the graphl query didn't specify an order so that pagination works correctly
            IValueNode orderByArgument = context.ArgumentLiteral<IValueNode>("order");
            if (orderByArgument == NullValueNode.Default || orderByArgument == null)
            {
                query = query.OrderBy(nsm => nsm.ModelUri).ThenBy(nsm => nsm.PublicationDate);
            }

            return query;
        }

    }
}
