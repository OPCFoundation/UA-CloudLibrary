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

    public class UANodesetResult : UANameSpace
    {
        [JsonProperty(PropertyName = "nodesetId")]
        public uint LegacyId { get => Nodeset?.Identifier ?? 0; }

        [JsonProperty(PropertyName = "nodesetTitle")]
        public string LegacyTitle { get => Title; }

        [JsonProperty(PropertyName = "orgName")]
        public string LegacyOrgName { get => Contributor?.Name; }

        // TODO enum vs. string & compat
        [JsonProperty(PropertyName = "version")]
        public string LegacyVersion { get => Nodeset?.Version; }

        [JsonProperty(PropertyName = "publicationDate")]
        public System.DateTime? LegacyPublicationDate { get => Nodeset?.PublicationDate; }

        [JsonProperty(PropertyName = "nodesetNamespaceUri")]
        public string LegacyNamespaceUri
        {
            get => Nodeset?.NamespaceUri?.OriginalString;
        }

        [JsonProperty(PropertyName = "requiredNodesets")]
        public List<CloudLibRequiredModelInfo> LegacyRequiredNodesets { get => Nodeset?.RequiredModels; }
    }
}
