using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UACloudLibClientLibrary.Models;

namespace UACloudLibClientLibrary
{
    static class ConvertMetadataToAddressspace
    {
        /// <summary>
        /// Converts metadata to a list of combinedtypes, taking the nodeset id from the metadata as a combination point
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        public static List<CombinatedTypes> Convert(List<MetadataResult> response)
        {
            List<CombinatedTypes> combinatedTypes = new List<CombinatedTypes>();

            foreach(MetadataResult metadata in response)
            {
                CombinatedTypes combinatedType = combinatedTypes?.FirstOrDefault(e => e.NodesetID == metadata.NodesetID);

                if (combinatedType == null)
                {
                    combinatedType = new CombinatedTypes(metadata.NodesetID);
                    combinatedTypes.Add(combinatedType);
                }
                ConvertCases(combinatedType, metadata);
            }
            return combinatedTypes;
        }
        /// <summary>
        /// Switch case with all the names for the members
        /// </summary>
        /// <param name="type"></param>
        /// <param name="metadata"></param>
        private static void ConvertCases(CombinatedTypes type, MetadataResult metadata)
        {
            switch (metadata.Name)
            {
                #region AdressSpace Cases
                case "adressspacemodifiedtime":
                    {
                        type.AddressSpace.LastModification = System.Convert.ToDateTime(metadata.Value);
                        break;
                    }
                case "addressspacedescription":
                    {
                        type.AddressSpace.Description = metadata.Value;
                        break;
                    }
                case "copyright":
                    {
                        type.AddressSpace.CopyrightText = metadata.Value;
                        break;
                    }
                case "documentationurl":
                    {
                        type.AddressSpace.DocumentationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "licenseurl":
                    {
                        type.AddressSpace.LicenseUrl = new Uri(metadata.Value);
                        break;
                    }
                case "purchasinginfo":
                    {
                        type.AddressSpace.PurchasingInformationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "keywords":
                    {
                        type.AddressSpace.KeyWords = metadata.Value.Split(",");
                        break;
                    }
                case "locales":
                    {
                        type.AddressSpace.SupportedLocales = metadata.Value.Split(",");
                        break;
                    }
                case "numdownloads":
                    {
                        type.AddressSpace.NumberOfDownloads = System.Convert.ToUInt32(metadata.Value);
                        break;
                    }
                case "addressspacename":
                    {
                        type.AddressSpace.Title = metadata.Value;
                        break;
                    }
                case "license":
                    {
                        // just for performance
                        switch (metadata.Value)
                        {
                            case "MIT":
                                {
                                    type.AddressSpace.License = AddressSpaceLicense.MIT;
                                    break;
                                }
                            case "ApacheLicense20":
                                {
                                    type.AddressSpace.License = AddressSpaceLicense.ApacheLicense20;
                                    break;
                                }
                            case "Custom":
                                {
                                    type.AddressSpace.License = AddressSpaceLicense.Custom;
                                    break;
                                }
                            default:
                                {
                                    type.AddressSpace.License = (AddressSpaceLicense)Enum.Parse(typeof(AddressSpaceLicense), metadata.Value);
                                    break;
                                }
                        }
                        break;
                    }
                case "version":
                    {
                        type.AddressSpace.Version = metadata.Value;
                        break;
                    }
                case "releasenotes":
                    {
                        type.AddressSpace.ReleaseNotesUrl = new Uri(metadata.Value);
                        break;
                    }
                case "testspecification":
                    {
                        type.AddressSpace.TestSpecificationUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                #region Organistion Cases
                case "orgname":
                    {
                        type.Organisation.Name = metadata.Value;
                        break;
                    }
                case "orgdesciption":
                    {
                        type.Organisation.Description = metadata.Value;
                        break;
                    }
                case "orgcontact":
                    {
                        type.Organisation.ContactEmail = metadata.Value;
                        break;
                    }
                case "orgwebsite":
                    {
                        type.Organisation.Website = new Uri(metadata.Value);
                        break;
                    }
                case "orglogo":
                    {
                        type.Organisation.LogoUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                case "nodesetmodifiedtime":
                    {
                        type.Nodeset.LastModification = System.Convert.ToDateTime(metadata.Value);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }

    public class CombinatedTypes
    {
        public long NodesetID { get; set; }
        public AddressSpace AddressSpace = new AddressSpace();
        public AddressSpaceCategory Category = new AddressSpaceCategory();
        public Organisation Organisation = new Organisation();
        public AddressSpaceNodeset2 Nodeset = new AddressSpaceNodeset2();

        public CombinatedTypes(long nodesetid)
        {
            NodesetID = nodesetid;
            AddressSpace.Contributor = Organisation;
            AddressSpace.Category = Category;
            AddressSpace.Nodeset = Nodeset;
        }

        public CombinatedTypes()
        {

        }
    }
}
