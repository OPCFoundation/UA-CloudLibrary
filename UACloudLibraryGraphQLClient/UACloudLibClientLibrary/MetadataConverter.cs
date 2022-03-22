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
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using UACloudLibClientLibrary.Models;
    using UACloudLibrary;

    static class MetadataConverter
    {
        /// <summary>
        /// Converts metadata to a list of combinedtypes, taking the nodeset id from the metadata as a combination point
        /// </summary>
        public static List<AddressSpace> Convert(List<MetadataResult> pageInfo)
        {
            Dictionary<string, AddressSpace> addressSpaces = new Dictionary<string, AddressSpace>();

            if (pageInfo != null)
            {
                foreach (MetadataResult item in pageInfo)
                {
                    string id = item.NodesetID.ToString();
                    if (!addressSpaces.ContainsKey(id))
                    {
                        addressSpaces.Add(id, new AddressSpace());
                    }
                    
                    ConvertCases(addressSpaces[id], item);
                }
            }

            return addressSpaces.Values.ToList();
        }

        public static List<AddressSpace> Convert(List<BasicNodesetInformation> infos)
        {
            List<AddressSpace> result = new List<AddressSpace>();

            if (infos != null)
            {
                foreach (BasicNodesetInformation info in infos)
                {
                    result.Add(Convert(info));
                }
            }

            return result;
        }

        public static AddressSpace Convert(BasicNodesetInformation info)
        {
            AddressSpace addressSpace = new AddressSpace();

            addressSpace.Title = info.Title;
            addressSpace.Version = info.Version;
            addressSpace.Contributor.Name = info.Organisation;
            addressSpace.License = info.License;
            addressSpace.Nodeset.PublicationDate = info.CreationTime;

            return addressSpace;
        }

        /// <summary>
        /// Converts with paging support so the UI dev doesn't have to deal with it
        /// </summary>
        public static List<AddressSpace> ConvertWithPaging(List<BasicNodesetInformation> infos, int limit = 10, int offset = 0)
        {
            List<AddressSpace> result = new List<AddressSpace>();
            
            if (limit == 0)
            {
                // return everything at once
                for (int i = offset; i < infos.Count; i++)
                {
                    result.Add(Convert(infos[i]));
                }
            }
            else if (limit > 0)
            {
                if (offset >= 0)
                {
                    for (int i = offset; (i < infos.Count) && ((i - offset) < limit); i++)
                    {
                        result.Add(Convert(infos[i]));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Switch case with all the names for the members
        /// </summary>
        private static void ConvertCases(AddressSpace addressspace, MetadataResult metadata)
        {
            switch (metadata.Name)
            {
                #region AdressSpace Cases
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
                        addressspace.Keywords = metadata.Value.Split(",");
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
                case "adressspacecreationtime":
                    {
                        addressspace.Nodeset.PublicationDate = DateTime.ParseExact(metadata.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
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
