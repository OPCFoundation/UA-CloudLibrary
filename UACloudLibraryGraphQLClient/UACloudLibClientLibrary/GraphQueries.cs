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
    using GraphQL.Query.Builder;
    using UACloudLibClientLibrary.Models;
    using UACloudLibrary;

    public class GraphQueries
    {
        public static IQuery<MetadataResult> MetadataQuery = new Query<MetadataResult>("metadata")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Name)
            .AddField(f => f.Value);

        public static IQuery<DataResult> DataQuery = new Query<DataResult>("datatype")
           .AddField(f => f.ID)
           .AddField(f => f.NodesetID)
           .AddField(f => f.Namespace)
           .AddField(f => f.Browsename)
           .AddField(f => f.Value);

        public static IQuery<ObjectResult> ObjectQuery = new Query<ObjectResult>("objecttype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);

        public static IQuery<ReferenceResult> ReferenceQuery = new Query<ReferenceResult>("referencetype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);

        public static IQuery<VariableResult> VariableQuery = new Query<VariableResult>("variabletype")
            .AddField(f => f.ID)
            .AddField(f => f.NodesetID)
            .AddField(f => f.Namespace)
            .AddField(f => f.Browsename)
            .AddField(f => f.Value);

        public static IQuery<AddressSpace> AddressSpaceQuery = new Query<AddressSpace>("addressspacetype")
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
                        .AddField(h => h.Description)
                        .AddField(h => h.IconUrl)
                )
            .AddField(h => h.Description)
            .AddField(h => h.DocumentationUrl)
            .AddField(h => h.PurchasingInformationUrl)
            .AddField(h => h.Version)
            .AddField(h => h.ReleaseNotesUrl)
            .AddField(h => h.Keywords)
            .AddField(h => h.SupportedLocales);

        public static IQuery<Organisation> OrganisationQuery = new Query<Organisation>("organisationtype")
            .AddField(f => f.Name)
            .AddField(f => f.Website)
            .AddField(f => f.ContactEmail)
            .AddField(f => f.Description)
            .AddField(f => f.LogoUrl);

        public static IQuery<AddressSpaceCategory> CategoryQuery = new Query<AddressSpaceCategory>("categorytype")
            .AddField(f => f.Name)
            .AddField(f => f.Description)
            .AddField(f => f.IconUrl);
    }
}
