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
    using System.ComponentModel.DataAnnotations;

    public enum License
    {
        MIT,
        ApacheLicense20,
        Custom
    }

    public class UANameSpace
    {
        public UANameSpace()
        {
            Title = string.Empty;
            License = License.Custom;
            CopyrightText = string.Empty;
            Contributor = new Organisation();
            Description = string.Empty;
            Category = new Category();
            Nodeset = new Nodeset();
            DocumentationUrl = null;
            IconUrl = null;
            LicenseUrl = null;
            Keywords = Array.Empty<string>();
            PurchasingInformationUrl = null;
            ReleaseNotesUrl = null;
            TestSpecificationUrl = null;
            SupportedLocales = Array.Empty<string>();
            NumberOfDownloads = 0;
            AdditionalProperties = null;
        }

        [Required]
        public string Title { get; set; }

        [Required]
        public License License { get; set; }

        [Required]
        public string CopyrightText { get; set; }

        [Required]
        public Organisation Contributor { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public Category Category { get; set; }

        [Required]
        public Nodeset Nodeset { get; set; }

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
        public string ValidationStatus { get; set; }

        public UAProperty[] AdditionalProperties { get; set; }
    }

    public class UAProperty
    {
        public string Name { get; set; }

        public string Value { get; set; }
    }

    public class Organisation
    {
        public Organisation()
        {
            Name = string.Empty;
            Description = null;
            LogoUrl = null;
            ContactEmail = null;
            Website = null;
        }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri LogoUrl { get; set; }

        public string ContactEmail { get; set; }

        public Uri Website { get; set; }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Organisation org = (Organisation)obj;
                return Name.Equals(org.Name, StringComparison.Ordinal);
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.Ordinal);
        }
    }

    public class Category
    {
        public Category()
        {
            Name = string.Empty;
            Description = null;
            IconUrl = null;
        }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public Uri IconUrl { get; set; }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                Category org = (Category)obj;
                return Name.Equals(org.Name, StringComparison.Ordinal);
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.Ordinal);
        }
    }

    public class Nodeset
    {
        public Nodeset()
        {
            NodesetXml = string.Empty;
            Identifier = 0;
            NamespaceUri = null;
            Version = string.Empty;
            PublicationDate = DateTime.MinValue;
            LastModifiedDate = DateTime.MinValue;
        }

        [Required]
        public string NodesetXml { get; set; }

        public uint Identifier { get; set; }

        public Uri NamespaceUri { get; set; }

        public string Version { get; set; }

        public DateTime PublicationDate { get; set; }

        public DateTime LastModifiedDate { get; set; }
        public string ValidationStatus { get; set; }

        public List<CloudLibRequiredModelInfo> RequiredModels { get; set; }
    }

    /// <summary>
    /// Contains information about dependencies of a nodeset
    /// </summary>
    public class CloudLibRequiredModelInfo
    {
        /// <summary>
        /// The namespace URI of the dependency
        /// </summary>
        public string NamespaceUri { get; set; }
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
        public Nodeset AvailableModel { get; set; }
    }
}
