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
    using System;
    
    public enum AddressSpaceLicense
    {
        MIT,
        ApacheLicense20,
        Custom
    }
    
 
    /// <summary>
    /// Contains the metadata of the nodeset and the nodeset itself
    /// </summary>
    public class AddressSpace
    {
        public AddressSpace()
        {
            this.Nodeset = new AddressSpaceNodeset2();
            this.Contributor = new Organisation();
            this.Category = new AddressSpaceCategory();
        }
        /// <summary>
        /// Used for identifying during conversion and when trying to download the nodeset
        /// </summary>
        public string MetadataID { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public AddressSpaceLicense License { get; set; }
        public string CopyrightText { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModificationTime { get; set; }
        public Organisation Contributor{ get; set; }
        public string Description { get; set; }
        public AddressSpaceCategory Category { get; set; }
        public AddressSpaceNodeset2 Nodeset { get; set; }
        /// <summary>
        /// If the address space is a official UA Information Model this refers to the OPC XYZ number of the published document. E.g. "OPC 40001-1" for the "UA CS for Machinery Part 1 - Basic Building Blocks"
        /// </summary>
        public Uri DocumentationUrl { get; set; }
        public Uri IconUrl { get; set; }
        public Uri LicenseUrl { get; set; }
        public string[] KeyWords { get; set; }
        public Uri PurchasingInformationUrl { get; set; }
        public Uri ReleaseNotesUrl { get; set; }
        public Uri TestSpecificationUrl { get; set; }
        /// <summary>
        /// Supported ISO language codes
        /// </summary>
        public string[] SupportedLocales { get; set; }
        public uint NumberOfDownloads { get; set; }
        public Tuple<string, string>[] AdditionalProperties { get; set; }
    }
 
    /// <summary>
    /// Contains the metadata for the contributor/organisation
    /// </summary>
    public class Organisation 
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri LogoUrl { get; set; }
        public string ContactEmail { get; set; }
        public Uri Website { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastModificationTime { get; set; }
    }
    
    /// <summary>
    /// Defines the category
    /// </summary>
    public class AddressSpaceCategory
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri IconUrl { get; set; }
        public DateTime CreationTimeStamp { get; set; }
        public DateTime LastModificationTime { get; set; }
    }
    
    /// <summary>
    /// Contains the nodeset and timestamps
    /// </summary>
    public class AddressSpaceNodeset2
    {
        public string NodesetXml { get; set; }
        public DateTime CreationTimeStamp { get; set; }
        public DateTime LastModification { get; set; }
    }
}