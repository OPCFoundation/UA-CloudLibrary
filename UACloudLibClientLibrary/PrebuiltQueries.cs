using System;
using System.Collections.Generic;
using System.Text;
using GraphQL.Query.Builder;
using UACloudLibClientLibrary.Models;


namespace UACloudLibClientLibrary
{
    /// <summary>
    /// Defines which fields are returned
    /// </summary>
    static class PrebuiltQueries
    {
        static string QueryWrapper(string query)
        {
            string test = string.Format("query{{{0}}}", query);
            return test;
        }

        static IQuery<DatatypeResult> _DatatypeQuery = new Query<DatatypeResult>("datatype")
           .AddField(f => f.ID)
           .AddField(f => f.NodesetID)
           .AddField(f => f.Namespace)
           .AddField(f => f.Browsename)
           .AddField(f => f.Value);

        public static string DatatypeQuery { get { return QueryWrapper(_DatatypeQuery.Build().ToString()); } }

        static IQuery<MetadataResult> _MetadataQuery = new Query<MetadataResult>("metadata")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Name)
            .AddField(f => f.Value);

        public static string MetadataQuery { get { return QueryWrapper(_MetadataQuery.Build().ToString()); } }

        static IQuery<ObjectResult> _ObjectQuery = new Query<ObjectResult>("objecttype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value)
            .AddField(f => f.Namespace);

        public static string ObjectQuery { get { return QueryWrapper(_ObjectQuery.Build().ToString()); } }

        static IQuery<ReferenceResult> _ReferenceQuery = new Query<ReferenceResult>("referencetype")
            .AddField(f => f.ID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);

        public static string ReferenceQuery { get { return QueryWrapper(_ReferenceQuery.Build().ToString()); } }

        static IQuery<VariableResult> _VariableQuery = new Query<VariableResult>("variabletype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Browsename)
            .AddField(f => f.Namespace)
            .AddField(f => f.Value);

        public static string VariableQuery { get { return QueryWrapper(_VariableQuery.Build().ToString()); } }
    }
}
