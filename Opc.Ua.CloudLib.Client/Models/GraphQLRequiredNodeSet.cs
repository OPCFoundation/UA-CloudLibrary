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

namespace Opc.Ua.Cloud.Library.Client
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;


    /// <summary>
    /// GraphQL version of the class
    /// </summary>
    internal class GraphQLRequiredNodeSet
    {
        public string Identifier { get; set; }
        public string ModelUri { get; set; }
        public string Version { get; set; }
        public DateTime? PublicationDate { get; set; }
        public string ValidationStatus { get; set; }

        // RequiredModels
        public List<GraphQlRequiredModelInfo> RequiredModels { get; set; } = new List<GraphQlRequiredModelInfo>();

        public Nodeset ToNodeSet()
        {
            return new Nodeset {
                NamespaceUri = new Uri(this.ModelUri),
                PublicationDate = this.PublicationDate ?? DateTime.MinValue,
                Identifier = uint.Parse(this.Identifier, CultureInfo.InvariantCulture),
                Version = this.Version,
                ValidationStatus = this.ValidationStatus,
                RequiredModels = this.RequiredModels?.Select(m => new RequiredModelInfo {
                    NamespaceUri = m.ModelUri,
                    PublicationDate = m.PublicationDate,
                    Version = m.Version,
                    AvailableModel = m.AvailableModel?.ToNodeSet(),
                }).ToList(),
            };
        }
    }
    /// <summary>
    /// Contains information about dependencies of a nodeset
    /// </summary>
    internal class GraphQlRequiredModelInfo
    {
        /// <summary>
        /// The namespace URI of the dependency
        /// </summary>
        public string ModelUri { get; set; }
        /// <summary>
        /// The minimum required publication date of the dependency
        /// </summary>
        public DateTime? PublicationDate { get; set; }
        /// <summary>
        /// The informational version of the dependency
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// The best match currently available in the cloud library. null if no match (no nodeset for this namespace uri or only node sets with older publication dates).
        /// </summary>
        public GraphQLRequiredNodeSet AvailableModel { get; set; }
    }
}
