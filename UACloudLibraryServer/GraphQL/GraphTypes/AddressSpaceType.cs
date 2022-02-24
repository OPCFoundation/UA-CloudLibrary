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

namespace UACloudLibrary
{
    using GraphQL.EntityFramework;
    using GraphQL.Types;

    public class AddressSpaceType : EfObjectGraphType<AppDbContext, AddressSpaceModel>
    {
        public AddressSpaceType(IEfGraphQLService<AppDbContext> graphQlService) : base(graphQlService)
        {
            Field(e => e.Title);
            Field(e => e.Version);
            Field(e => e.Description);
            Field(e => e.CopyrightText);
            Field(e => e.NumberOfDownloads);
            Field(e => e.CreationTime);
            Field(e => e.LastModificationTime);
            Field(e => e.SupportedLocales);
            Field(e => e.Keywords);
            Field(e => e.License);
            Field(e => e.Nodeset_Id);
            AddNavigationField(name: "Contributor", resolve: e => e.DbContext.organisation.Find(e.Source.Contributor_Id));
            AddNavigationField(name: "Category", resolve: e => e.DbContext.category.Find(e.Source.Category_Id));
            AddNavigationField(name: "LicenseUrl", resolve: e => e.Source.LicenseUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "DocumentationUrl", resolve: e => e.Source.DocumentationUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "IconUrl", resolve: e => e.Source.IconUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "PurchasingInformationUrl", resolve: e => e.Source.PurchasingInformationUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "ReleaseNotesUrl", resolve: e => e.Source.ReleaseNotesUrl, graphType: typeof(UriGraphType));
            AddNavigationField(name: "TestSpecificationUrl", resolve: e => e.Source.TestSpecificationUrl, graphType: typeof(UriGraphType));
        }
    }
}
