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

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Opc.Ua.Cloud.Library.Models
{
    [Table("NamespaceMeta")]
    public partial class NamespaceMetaDataModel
    {
        public string NodesetId { get; set; }

        public DateTime CreationTime { get; set; }

        public virtual CloudLibNodeSetModel NodeSet { get; set; }

        public string Title { get; set; }

        public string License { get; set; }

        public string CopyrightText { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Link to additional documentation, specifications, GitHub, etc.
        /// For example, If the address space is based on a standard or official UA Information Model, this links to the standard or the OPC specification URL.
        /// </summary>
        public string DocumentationUrl { get; set; }

        public string IconUrl { get; set; }

        public string LicenseUrl { get; set; }

        public string[] Keywords { get; set; }

        public string PurchasingInformationUrl { get; set; }

        public string ReleaseNotesUrl { get; set; }

        public string TestSpecificationUrl { get; set; }

        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }

        public uint NumberOfDownloads { get; set; }

        public ValidationStatus ValidationStatus => NodeSet?.ValidationStatus ?? ValidationStatus.Parsed;

        public ApprovalStatus? ApprovalStatus { get; set; }

        public string ApprovalInformation { get; set; }

        public string UserId { get; set; }
    }
}
