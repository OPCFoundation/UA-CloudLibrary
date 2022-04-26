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
    using GraphQL.Types;
    using UACloudLibrary.Models;

    public class NameSpaceType : ObjectGraphType<UANameSpace>
    {
        public NameSpaceType()
        {
            Field(e => e.Title, type: typeof(NonNullGraphType<StringGraphType>));
            Field(e => e.License, type: typeof(NonNullGraphType<NameSpaceLicenseType>));
            Field(e => e.CopyrightText, type: typeof(NonNullGraphType<StringGraphType>));
            Field(e => e.Contributor, type: typeof(NonNullGraphType<OrganisationType>));
            Field(e => e.Description, type: typeof(NonNullGraphType<StringGraphType>));
            Field(e => e.Category, type: typeof(NonNullGraphType<CategoryType>));
            Field(e => e.Nodeset, type: typeof(NonNullGraphType<NodesetType>));
            Field(e => e.NumberOfDownloads, type: typeof(UIntGraphType));
            Field(e => e.SupportedLocales, type: typeof(ListGraphType<StringGraphType>));
            Field(e => e.Keywords, type: typeof(ListGraphType<StringGraphType>));
            Field(e => e.LicenseUrl, type: typeof(UriGraphType));
            Field(e => e.DocumentationUrl, type: typeof(UriGraphType));
            Field(e => e.IconUrl, type: typeof(UriGraphType));
            Field(e => e.PurchasingInformationUrl, type: typeof(UriGraphType));
            Field(e => e.ReleaseNotesUrl, type: typeof(UriGraphType));
            Field(e => e.TestSpecificationUrl, type: typeof(UriGraphType));
        }
    }
}