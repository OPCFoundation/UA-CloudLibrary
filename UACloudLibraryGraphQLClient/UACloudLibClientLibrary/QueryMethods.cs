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

namespace UACloudLibClientLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Finalizes the queries
    /// </summary>
    static class QueryMethods
    {
        const string strEndQuery = "}";

        /// <summary>
        /// Prepares and finalizes the AddressSpace query
        /// </summary>
        /// <param name="after"></param>
        /// <param name="first"></param>
        /// <param name="orFilter"></param>
        /// <param name="filter"></param>
        /// <returns>The finalized query</returns>
        public static string AddressSpacesQuery(int limit, int offset, IEnumerable<AddressSpaceWhereExpression> filter = null, GroupedOrExpression<AddressSpaceSearchField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{");

            query.AppendLine($"addressspacetype(limit: {limit}, offset: {offset})");

            if (orFilter != null)
            {
                query.Append(WhereExpressionBuilder(filter));
            }

            query.AppendLine(PrebuiltQueries.AddressSpaceQuery);

            query.AppendLine(strEndQuery);

            return query.ToString();
        }

        /// <summary>
        /// Creates the addressspace query with a searchtext
        /// </summary>
        /// <param name="after"></param>
        /// <param name="first"></param>
        /// <param name="searchtext"></param>
        /// <returns>The finalized query</returns>
        public static string AddressSpaceQuery(int limit, int offset, string searchtext)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{");

            query.AppendLine($"addressspacetype(limit: {limit}, offset: {offset})");

            query.AppendLine(PrebuiltQueries.AddressSpaceQuery);

            query.AppendLine(strEndQuery);

            return query.ToString();
        }
        /// <summary>
        /// Prepares the query string for the categories
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="andFilter"></param>
        /// <param name="orFilter"></param>
        /// <returns></returns>
        public static string QueryCategories(int limit, int offset, IEnumerable<CategoryWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{");

            query.AppendLine($"categorytype(limit: {limit}, offset: {offset})");

            query.Append(WhereExpressionBuilder(andFilter));

            query.AppendLine(PrebuiltQueries.CategoryQuery);

            query.AppendLine(strEndQuery);

            return query.ToString();
        }

        /// <summary>
        /// Prepares and finalizes the organisations query
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="after"></param>
        /// <param name="andFilter"></param>
        /// <param name="orFilter"></param>
        /// <returns></returns>
        public static string QueryOrganisations(int limit, int offset, IEnumerable<OrganisationWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{");

            query.AppendLine($"organisationtype(limit: {limit}, offset: {offset})");

            if (andFilter != null)
            {
                query.Append(WhereExpressionBuilder(andFilter));
            }

            // Fills the query with the requested properties
            query.AppendLine(PrebuiltQueries.OrganisationsQuery);

            query.AppendLine(strEndQuery);

            return query.ToString();
        }

        public static string QueryMetadata(IEnumerable<MetadataWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{metadata");

            if (andFilter != null)
            {
                query.Append("(");
                query.Append(WhereExpressionBuilder(andFilter));
                query.Append(")");
            }

            query.Append(PrebuiltQueries.MetadataQuery);

            query.Append(strEndQuery);

            return query.ToString();
        }

        public static string QueryObjectType(IEnumerable<ObjectTypeWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{objecttype");
 
            if (andFilter != null)
            {
                query.Append("(");
                query.Append(WhereExpressionBuilder(andFilter));
                query.Append(")");
            }

            query.Append(PrebuiltQueries.ObjectQuery);

            query.Append(strEndQuery);

            return query.ToString();
        }

        public static string QueryReferences()
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{referencetype");

            query.Append(PrebuiltQueries.ReferenceQuery);

            query.Append(strEndQuery);

            return query.ToString();
        }

        public static string QueryVariables()
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{variabletype");

            query.Append(PrebuiltQueries.VariableQuery);

            query.Append(strEndQuery);

            return query.ToString();
        }

        public static string QueryDatatypes()
        {
            StringBuilder query = new StringBuilder();

            query.AppendLine("query{datatypes");

            query.Append(PrebuiltQueries.DatatypeQuery);

            query.Append(strEndQuery);

            return query.ToString();
        }

        /// <summary>
        /// Checks if a clause is available and finalizes the statement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="filter"></param>
        /// <param name="orFilter"></param>
        /// <returns>Returns an empty string when no clause was transfered, otherwise the finalized where statement</returns>
        private static string WhereExpressionBuilder<T>(IEnumerable<IWhereExpression<T>> filter)
            where T : Enum
        {
            StringBuilder query = new StringBuilder();

            if (filter.Any())
            {
                return "";
            }
            else
            {
                query.Append(", where: [");

                if (filter != null)
                {
                    query.Append(string.Format(",", filter));
                }

                query.Append("]");

                return query.ToString();
            }
        }
    }
}
