﻿using GraphQL.Query.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UACloudLibClientLibrary
{
    /// <summary>
    /// Finalizes the queries
    /// </summary>
    static class QueryMethods
    {
        const string strEndQuery = "}" +
                "pageInfo{" +
                "hasNextPage " +
                "hasPreviousPage" +
                "}" +
                "totalCount" +
                "}}";

        /// <summary>
        /// Prepares and finalizes the AddressSpace query
        /// </summary>
        /// <param name="after"></param>
        /// <param name="first"></param>
        /// <param name="orFilter"></param>
        /// <param name="andFilter"></param>
        /// <returns>The finalized query</returns>
        public static string AddressSpacesQuery(string after, int first, List<AddressSpaceWhereExpression> andFilter = null, GroupedOrExpression<AddressSpaceSearchField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"addressspace(after: \"{after}\", first: {first}");

            if (orFilter != null)
            {
                query.Append(WhereExpressionBuilder(andFilter, orFilter));
            }

            query.AppendLine("){" +
                "edges{" +
                "cursor ");

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
        public static string AddressSpaceQuery(string after, int first, string searchtext)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"addressspace(after: \"{after}\", first: {first}, searchtext: \"{searchtext}\"");

            query.AppendLine("){" +
                "edges{" +
                "cursor ");

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
        public static string QueryCategories(int pageSize, string after, List<CategoryWhereExpression> andFilter = null, GroupedOrExpression<CategorySearchField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"category(after: \"{after}\", first: {pageSize}");

            query.Append(WhereExpressionBuilder(andFilter, orFilter));

            query.AppendLine("){" +
                "edges{" +
                "cursor ");

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
        public static string QueryOrganisations(int pageSize, string after, List<OrganisationWhereExpression> andFilter = null, GroupedOrExpression<OrganisationSearchField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"organisation(after: \"{after}\", first: {pageSize}");
            if (andFilter != null || orFilter != null)
            {
                query.Append(WhereExpressionBuilder(andFilter, orFilter));
            }

            query.AppendLine("){" +
                "edges{" +
                "cursor ");

            // Fills the query with the requested properties
            query.AppendLine(PrebuiltQueries.OrganisationsQuery);

            query.AppendLine(strEndQuery);

            return query.ToString();
        }

        public static string QueryMetadata(List<MetadataWhereExpression> andFilter = null, GroupedOrExpression<MetadataField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{metadata");
            if(andFilter != null || orFilter != null)
            {
                query.Append("(");
                query.Append(WhereExpressionBuilder(andFilter, orFilter));
                query.Append(")");
            }
            query.Append("{" + PrebuiltQueries.MetadataQuery + "}}");
            return query.ToString();
        }

        public static string QueryObjectType(List<ObjectTypeWhereExpression> andFilter = null, GroupedOrExpression<ObjectTypeFields> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{objecttype");
            if (andFilter != null || orFilter != null)
            {
                query.Append("(");
                query.Append(WhereExpressionBuilder(andFilter, orFilter));
                query.Append(")");
            }
            query.Append("{" + PrebuiltQueries.ObjectQuery + "}}");
            return query.ToString();
        }

        public static string QueryReferences()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{referencetype");
            query.Append("{" + PrebuiltQueries.ReferenceQuery + "}}");
            return query.ToString();
        }

        public static string QueryVariables()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{variabletype");
            query.Append("{" + PrebuiltQueries.VariableQuery + "}}");
            return query.ToString();
        }

        public static string QueryDatatypes()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{datatypes");
            query.Append("{" + PrebuiltQueries.DatatypeQuery + "}}");
            return query.ToString();
        }

        /// <summary>
        /// Checks if a clause is available and finalizes the statement
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="andFilter"></param>
        /// <param name="orFilter"></param>
        /// <returns>Returns an empty string when no clause was transfered, otherwise the finalized where statement</returns>
        private static string WhereExpressionBuilder<T, U>(T andFilter, GroupedOrExpression<U> orFilter = null)
                                where U : Enum
                                where T : IEnumerable<IWhereExpression<U>>
        {
            StringBuilder query = new StringBuilder();
            if (andFilter.Count() == 0 && orFilter == null)
            {
                return "";
            }
            else
            {
                query.Append(", where: [");
                if (andFilter != null && orFilter != null)
                {
                    foreach (IWhereExpression<U> clause in andFilter)
                    {
                        query.Append(clause.GetExpression() + ",");
                    }
                    query.Append(orFilter.GetGroupedExpression());
                }
                else if (andFilter != null)
                {
                    IWhereExpression<U> last = andFilter.Last();
                    foreach (IWhereExpression<U> clause in andFilter)
                    {
                        if (clause.Equals(last))
                        {
                            query.Append(clause.GetExpression());
                        }
                        else
                        {
                            query.Append(clause.GetExpression() + ",");
                        }
                    }
                }
                else
                {
                    query.Append(orFilter.GetGroupedExpression());
                }
                query.Append("]");
                return query.ToString();
            }
        }
    }
}