namespace UACloudLibClientLibrary
{
    using GraphQL.Query.Builder;
    /// <summary>
    /// Defines which fields are returned
    /// </summary>
    internal static class PrebuiltQueries
    {
        private static string QueryWrapper(string query)
        {
            string test = string.Format("query{{{0}}}", query);
            return test;
        }
        public static string DatatypeQuery { get { return _DatatypeQuery.Build().ToString(); } }
        #region datatypequery
        static IQuery<DatatypeResult> _DatatypeQuery = new Query<DatatypeResult>("node")
           .AddField(f => f.ID)
           .AddField(f => f.NodesetID)
           .AddField(f => f.Namespace)
           .AddField(f => f.Browsename)
           .AddField(f => f.Value);
        #endregion

        public static string MetadataQuery { get { return _MetadataQuery.Build().ToString(); } }
        #region metadataquery
        static IQuery<MetadataResult> _MetadataQuery = new Query<MetadataResult>("node")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Name)
            .AddField(f => f.Value);
        #endregion

        public static string ObjectQuery { get { return _ObjectQuery.Build().ToString(); } }
        #region objectquery
        static IQuery<ObjectResult> _ObjectQuery = new Query<ObjectResult>("node")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value)
            .AddField(f => f.Namespace);
        #endregion

        public static string ReferenceQuery { get { return _ReferenceQuery.Build().ToString(); } }
        #region referencequery
        static IQuery<ReferenceResult> _ReferenceQuery = new Query<ReferenceResult>("node")
            .AddField(f => f.ID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);
        #endregion

        public static string VariableQuery { get { return _VariableQuery.Build().ToString(); } }
        #region variablequery
        static IQuery<VariableResult> _VariableQuery = new Query<VariableResult>("node")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Browsename)
            .AddField(f => f.Namespace)
            .AddField(f => f.Value);
        #endregion

        public static string AddressSpaceQuery { get { return _AddressSpaceQuery.Build().ToString(); } }
        #region addressspace
        static IQuery<AddressSpace> _AddressSpaceQuery = new Query<AddressSpace>("node")
            .AddField(h => h.Title)
            .AddField(
                h => h.Contributor,
                sq => sq.AddField(h => h.Name)
                        .AddField(h => h.ContactEmail)
                        .AddField(h => h.Website)
                        .AddField(h => h.LogoUrl)
                        .AddField(h => h.Description)
                )
            .AddField(h => h.License)
            .AddField(
                h => h.Category,
                sq => sq.AddField(h => h.Name)
                        .AddField(h => h.LastModificationTime)
                        .AddField(h => h.IconUrl)
                )
            .AddField(h => h.Description)
            .AddField(h => h.DocumentationUrl)
            .AddField(h => h.PurchasingInformationUrl)
            .AddField(h => h.Version)
            .AddField(h => h.ReleaseNotesUrl)
            .AddField(h => h.KeyWords)
            .AddField(h => h.SupportedLocales)
            .AddField(h => h.LastModificationTime);
        #endregion

        public static string OrganisationsQuery { get { return _OrgQuery.Build().ToString(); } }
        #region organisation
        static IQuery<Organisation> _OrgQuery = new Query<Organisation>("node")
            .AddField(f => f.Name)
            .AddField(f => f.Website)
            .AddField(f => f.ContactEmail)
            .AddField(f => f.Description)
            .AddField(f => f.LogoUrl);
        #endregion

        public static string CategoryQuery { get { return _CategoryQuery.Build().ToString(); } }
        #region category
        static IQuery<AddressSpaceCategory> _CategoryQuery = new Query<AddressSpaceCategory>("node")
            .AddField(f => f.Name)
            .AddField(f => f.Description)
            .AddField(f => f.IconUrl)
            .AddField(f => f.LastModificationTime);
        #endregion
    }
}
