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
    using GraphQL;
    using GraphQL.Types;

    public class UaCloudLibQuery : ObjectGraphType
    {
        public UaCloudLibQuery(UaCloudLibResolver cloudLibResolver)
        {
            Name = "UACloudLibraryQuery";

            Field<ListGraphType<DatatypeType>>("dataType", resolve: context => cloudLibResolver.GetDataTypes());

            Field<ListGraphType<MetadataType>>("metadata", resolve: context => cloudLibResolver.GetMetaData());

            Field<ListGraphType<ObjecttypeType>>("objectType", resolve: context => cloudLibResolver.GetObjectTypes());

            Field<ListGraphType<ReferencetypeType>>("referenceType", resolve: context => cloudLibResolver.GetReferenceTypes());

            Field<ListGraphType<VariabletypeType>>("variableType", resolve: context => cloudLibResolver.GetVariableTypes());

            Field<ListGraphType<NodesetType>>("nodeset", resolve: context => cloudLibResolver.GetNodesetTypes());

            Field<ListGraphType<CategoryType>>(
                "category",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<IntGraphType> { Name = "offset" },
                    new QueryArgument<StringGraphType> { Name = "where" },
                    new QueryArgument<StringGraphType> { Name = "orderBy" }
                ),
                resolve: context => {
                    int limit = context.GetArgument("limit", 1000000);
                    string where = context.GetArgument("where", string.Empty);
                    string orderBy = context.GetArgument("orderBy", string.Empty);
                    return cloudLibResolver.GetCategoryTypes(limit, where, orderBy);
                }
            );

            Field<ListGraphType<OrganisationType>>(
                "organisation",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<IntGraphType> { Name = "offset" },
                    new QueryArgument<StringGraphType> { Name = "where" },
                    new QueryArgument<StringGraphType> { Name = "orderBy" }
                ),
                resolve: context => {
                    int limit = context.GetArgument("limit", 1000000);
                    string where = context.GetArgument("where", string.Empty);
                    string orderBy = context.GetArgument("orderBy", string.Empty);
                    return cloudLibResolver.GetOrganisationTypes(limit, where, orderBy);
                }
            );

            Field<ListGraphType<NameSpaceType>>(
                "nameSpace",
                arguments: new QueryArguments(
                    new QueryArgument<IntGraphType> { Name = "limit" },
                    new QueryArgument<IntGraphType> { Name = "offset" },
                    new QueryArgument<StringGraphType> { Name = "where" },
                    new QueryArgument<StringGraphType> { Name = "orderBy" }
                ),
                resolve: context => {
                    int limit = context.GetArgument("limit", 1000000);
                    int offset = context.GetArgument("offset", 0);
                    string where = context.GetArgument("where", string.Empty);
                    string orderBy = context.GetArgument("orderBy", string.Empty);
                    return cloudLibResolver.GetNameSpaceTypes(limit, offset, where, orderBy);
                }
            );
        }
    }
}
