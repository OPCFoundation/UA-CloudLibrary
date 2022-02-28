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
        /// <param name="filter"></param>
        /// <returns>The finalized query</returns>
        public static string AddressSpacesQuery(string after, int first, IEnumerable<AddressSpaceWhereExpression> filter = null, GroupedOrExpression<AddressSpaceSearchField> orFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"addressspace(after: \"{after}\", first: {first}");

            if (orFilter != null)
            {
                query.Append(WhereExpressionBuilder(filter));
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
        public static string QueryCategories(int pageSize, string after, IEnumerable<CategoryWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"category(after: \"{after}\", first: {pageSize}");

            query.Append(WhereExpressionBuilder(andFilter));

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
        public static string QueryOrganisations(int pageSize, string after, IEnumerable<OrganisationWhereExpression> andFilter = null)
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{");
            query.AppendLine($"organisation(after: \"{after}\", first: {pageSize}");
            if (andFilter != null)
            {
                query.Append(WhereExpressionBuilder(andFilter));
            }

            query.AppendLine("){" +
                "edges{" +
                "cursor ");

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
            query.AppendLine("{edges{cursor");
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

            query.AppendLine("{edges{cursor");
            query.Append(PrebuiltQueries.ObjectQuery);
            query.Append(strEndQuery);
            return query.ToString();
        }

        public static string QueryReferences()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{referencetype");

            query.AppendLine("{edges{cursor");
            query.Append(PrebuiltQueries.ReferenceQuery);
            query.Append(strEndQuery);
            return query.ToString();
        }

        public static string QueryVariables()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{variabletype");

            query.AppendLine("{edges{cursor");
            query.Append(PrebuiltQueries.VariableQuery);
            query.Append(strEndQuery);
            return query.ToString();
        }

        public static string QueryDatatypes()
        {
            StringBuilder query = new StringBuilder();
            query.AppendLine("query{datatypes");

            query.AppendLine("{edges{cursor");
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
