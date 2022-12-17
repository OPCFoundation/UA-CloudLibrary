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

namespace Opc.Ua.Cloud.Library.DbContextModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Globalization;
    using System.Linq;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;
    using Opc.Ua.Cloud.Library.Models;

    [Table("NamespaceMeta")]
    public partial class NamespaceMetaDataModel
    {
        public string NodesetId { get; set; }
        public virtual CloudLibNodeSetModel NodeSet { get; set; }
        public string Title { get; set; }
        public int ContributorId { get; set; }
        public virtual OrganisationModel Contributor { get; set; }
        public string License { get; set; }
        public string CopyrightText { get; set; }
        public string Description { get; set; }
        public int CategoryId { get; set; }
        public virtual CategoryModel Category { get; set; }
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
        public virtual List<AdditionalPropertyModel> AdditionalProperties { get; set; }


        public static void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<OrganisationModel>()
                .HasKey(o => o.Id);
            builder.Entity<OrganisationModel>()
                .HasIndex(o => o.Name).IsUnique();

            builder.Entity<CategoryModel>()
                .HasKey(c => c.Id);
            builder.Entity<CategoryModel>()
                .HasIndex(c => c.Name).IsUnique();

            builder.Entity<NamespaceMetaDataModel>()
                .Ignore(md => md.ValidationStatus)
                .HasKey(n => n.NodesetId)
                ;

            builder.Entity<NamespaceMetaDataModel>()
                .Property(nsm => nsm.ApprovalStatus)
                    .HasConversion<string>();

            builder.Entity<NamespaceMetaDataModel>()
                .HasOne(md => md.NodeSet).WithOne(nm => nm.Metadata)
                    .HasForeignKey<CloudLibNodeSetModel>(nm => nm.Identifier)
                    .IsRequired(false)
                    .OnDelete(DeleteBehavior.NoAction)
                    ;
            builder.Entity<NamespaceMetaDataModel>()
                .OwnsMany(n => n.AdditionalProperties).WithOwner(md => md.NodeSet);

            builder.Entity<NamespaceMetaDataModel>()
                .HasOne(n => n.Category).WithMany();
            builder.Entity<NamespaceMetaDataModel>()
                .HasOne(n => n.Contributor).WithMany();
            builder.Entity<NamespaceMetaDataModel>()
                .HasIndex(md => new { md.Title, md.Description, /*md.Keywords, md.Category.Name, md.Contributor*/ })
                .HasMethod("GIN")
                .IsTsVectorExpressionIndex("english")
                ;
        }
    }

    [Table("AdditionalProperties")]
    [Owned]
    public partial class AdditionalPropertyModel
    {
        public string NodeSetId { get; set; }
        public virtual NamespaceMetaDataModel NodeSet { get; set; }
        public string Name { get; set; }

        public string Value { get; set; }

    }


    public class OrganisationModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LogoUrl { get; set; }
        public string ContactEmail { get; set; }
        public string Website { get; set; }
    }

    public class CategoryModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
    }
}
