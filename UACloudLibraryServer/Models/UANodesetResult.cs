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

namespace Opc.Ua.Cloud.Library.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class UANodesetResult
    {
        [JsonProperty(PropertyName = "nodesetId")]
        public uint Id { get; set; }

        [JsonProperty(PropertyName = "nodesetTitle")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "orgName")]
        public string Contributor { get; set; }

        [JsonProperty(PropertyName = "license")]
        public string License { get; set; }

        public string CopyrightText { get; set; }

        public string Description { get; set; }

        public Category Category { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        public Uri DocumentationUrl { get; set; }

        public Uri IconUrl { get; set; }

        public Uri LicenseUrl { get; set; }

        public string[] Keywords { get; set; }

        public Uri PurchasingInformationUrl { get; set; }

        public Uri ReleaseNotesUrl { get; set; }

        public Uri TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }

        public uint NumberOfDownloads { get; set; }
        [JsonProperty(PropertyName = "validationStatus")]
        public string ValidationStatus { get; set; }


        public UAProperty[] AdditionalProperties { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "publicationDate")]
        public System.DateTime? PublicationDate { get; set; }

        [JsonProperty(PropertyName = "nodesetNamespaceUri")]
        public string NameSpaceUri { get; set; }

        public List<CloudLibRequiredModelInfo> RequiredNodesets { get; set; }

    }
}
