using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static List<AddressSpace> Convert(List<MetadataResult> response)
        {
            List<AddressSpace> listAddressSpaces = new List<AddressSpace>();

            foreach(MetadataResult metadata in response)
            {
                string id = metadata.NodesetID.ToString();
                AddressSpace addressspace = listAddressSpaces?.FirstOrDefault(e => e.MetadataID == id);

                if (addressspace == null)
                {
                    addressspace = new AddressSpace();
                    addressspace.MetadataID = id;
                    listAddressSpaces.Add(addressspace);
                }
                ConvertCases(addressspace, metadata);
            }
            return listAddressSpaces;
        }
        /// <summary>
        /// Switch case with all the names for the members
        /// </summary>
        /// <param name="addressspace"></param>
        /// <param name="metadata"></param>
        private static void ConvertCases(AddressSpace addressspace, MetadataResult metadata)
        {
            switch (metadata.Name)
            {
                #region AdressSpace Cases
                case "adressspacemodifiedtime":
                    {
                        addressspace.LastModificationTime = DateTime.ParseExact(metadata.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        break;
                    }
                case "addressspacedescription":
                    {
                        addressspace.Description = metadata.Value;
                        break;
                    }
                case "copyright":
                    {
                        addressspace.CopyrightText = metadata.Value;
                        break;
                    }
                case "documentationurl":
                    {
                        addressspace.DocumentationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "licenseurl":
                    {
                        addressspace.LicenseUrl = new Uri(metadata.Value);
                        break;
                    }
                case "purchasinginfo":
                    {
                        addressspace.PurchasingInformationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "keywords":
                    {
                        addressspace.KeyWords = metadata.Value.Split(",");
                        break;
                    }
                case "locales":
                    {
                        addressspace.SupportedLocales = metadata.Value.Split(",");
                        break;
                    }
                case "numdownloads":
                    {
                        addressspace.NumberOfDownloads = System.Convert.ToUInt32(metadata.Value);
                        break;
                    }
                case "addressspacename":
                    {
                        addressspace.Title = metadata.Value;
                        break;
                    }
                case "license":
                    {
                        // just for performance
                        switch (metadata.Value)
                        {
                            case "MIT":
                                {
                                    addressspace.License = AddressSpaceLicense.MIT;
                                    break;
                                }
                            case "ApacheLicense20":
                                {
                                    addressspace.License = AddressSpaceLicense.ApacheLicense20;
                                    break;
                                }
                            case "Custom":
                                {
                                    addressspace.License = AddressSpaceLicense.Custom;
                                    break;
                                }
                            default:
                                {
                                    addressspace.License = (AddressSpaceLicense)Enum.Parse(typeof(AddressSpaceLicense), metadata.Value);
                                    break;
                                }
                        }
                        break;
                    }
                case "version":
                    {
                        addressspace.Version = metadata.Value;
                        break;
                    }
                case "releasenotes":
                    {
                        addressspace.ReleaseNotesUrl = new Uri(metadata.Value);
                        break;
                    }
                case "testspecification":
                    {
                        addressspace.TestSpecificationUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                #region Organistion Cases
                case "orgname":
                    {
                        addressspace.Contributor.Name = metadata.Value;
                        break;
                    }
                case "orgdesciption":
                    {
                        addressspace.Contributor.Description = metadata.Value;
                        break;
                    }
                case "orgcontact":
                    {
                        addressspace.Contributor.ContactEmail = metadata.Value;
                        break;
                    }
                case "orgwebsite":
                    {
                        addressspace.Contributor.Website = new Uri(metadata.Value);
                        break;
                    }
                case "orglogo":
                    {
                        addressspace.Contributor.LogoUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                case "nodesetmodifiedtime":
                    {
                        addressspace.Nodeset.LastModification = DateTime.ParseExact(metadata.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
    }
}
