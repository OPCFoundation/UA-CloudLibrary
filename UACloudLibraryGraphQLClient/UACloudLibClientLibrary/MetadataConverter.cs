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
    using UACloudLibrary.Models;

    static class MetadataConverter
    {
        /// <summary>
        /// Converts metadata to a list of combinedtypes, taking the nodeset id from the metadata as a combination point
        /// </summary>
        public static List<AddressSpace> Convert(List<MetadataResult> metadata)
        {
            Dictionary<string, AddressSpace> addressSpaces = new Dictionary<string, AddressSpace>();

            if (metadata != null)
            {
                foreach (MetadataResult item in metadata)
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

        public static List<AddressSpace> Convert(List<UANodesetResult> infos)
        {
            List<AddressSpace> result = new List<AddressSpace>();

            if (infos != null)
            {
                foreach (UANodesetResult info in infos)
                {
                    result.Add(Convert(info));
                }
            }

            return result;
        }

        public static AddressSpace Convert(UANodesetResult info)
        {
            AddressSpace addressSpace = new AddressSpace();

            addressSpace.Title = info.Title;
            addressSpace.Nodeset.Version = info.Version;
            addressSpace.Contributor.Name = info.Contributor;
            
            switch (info.License)
            {
                case "MIT":
                    {
                        addressSpace.License = License.MIT;
                        break;
                    }
                case "ApacheLicense20":
                    {
                        addressSpace.License = License.ApacheLicense20;
                        break;
                    }
                case "Custom":
                    {
                        addressSpace.License = License.Custom;
                        break;
                    }
                default:
                    {
                        addressSpace.License = License.Custom;
                        break;
                    }
            }

            addressSpace.Nodeset.PublicationDate = (info.CreationTime != null)? info.CreationTime.Value : DateTime.MinValue;

            return addressSpace;
        }

        /// <summary>
        /// Converts with paging support so the UI dev doesn't have to deal with it
        /// </summary>
        public static List<AddressSpace> ConvertWithPaging(List<UANodesetResult> infos, int limit = 10, int offset = 0)
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
        private static void ConvertCases(AddressSpace addressSpace, MetadataResult metadata)
        {
            switch (metadata.Name)
            {
                #region AdressSpace Cases
                case "addressspacedescription":
                    {
                        addressSpace.Description = metadata.Value;
                        break;
                    }
                case "copyright":
                    {
                        addressSpace.CopyrightText = metadata.Value;
                        break;
                    }
                case "documentationurl":
                    {
                        addressSpace.DocumentationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "licenseurl":
                    {
                        addressSpace.LicenseUrl = new Uri(metadata.Value);
                        break;
                    }
                case "purchasinginfo":
                    {
                        addressSpace.PurchasingInformationUrl = new Uri(metadata.Value);
                        break;
                    }
                case "keywords":
                    {
                        addressSpace.Keywords = metadata.Value.Split(',');
                        break;
                    }
                case "locales":
                    {
                        addressSpace.SupportedLocales = metadata.Value.Split(',');
                        break;
                    }
                case "numdownloads":
                    {
                        addressSpace.NumberOfDownloads = System.Convert.ToUInt32(metadata.Value);
                        break;
                    }
                case "addressspacename":
                    {
                        addressSpace.Title = metadata.Value;
                        break;
                    }
                case "license":
                    {
                        switch (metadata.Value)
                        {
                            case "MIT":
                                {
                                    addressSpace.License = License.MIT;
                                    break;
                                }
                            case "ApacheLicense20":
                                {
                                    addressSpace.License = License.ApacheLicense20;
                                    break;
                                }
                            case "Custom":
                                {
                                    addressSpace.License = License.Custom;
                                    break;
                                }
                            default:
                                {
                                    addressSpace.License = License.Custom;
                                    break;
                                }
                        }
                        break;
                    }
                case "version":
                    {
                        addressSpace.Nodeset.Version = metadata.Value;
                        break;
                    }
                case "releasenotes":
                    {
                        addressSpace.ReleaseNotesUrl = new Uri(metadata.Value);
                        break;
                    }
                case "testspecification":
                    {
                        addressSpace.TestSpecificationUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                #region Organistion Cases
                case "orgname":
                    {
                        addressSpace.Contributor.Name = metadata.Value;
                        break;
                    }
                case "orgdesciption":
                    {
                        addressSpace.Contributor.Description = metadata.Value;
                        break;
                    }
                case "orgcontact":
                    {
                        addressSpace.Contributor.ContactEmail = metadata.Value;
                        break;
                    }
                case "orgwebsite":
                    {
                        addressSpace.Contributor.Website = new Uri(metadata.Value);
                        break;
                    }
                case "orglogo":
                    {
                        addressSpace.Contributor.LogoUrl = new Uri(metadata.Value);
                        break;
                    }
                #endregion
                case "adressspacecreationtime":
                    {
                        addressSpace.Nodeset.PublicationDate = DateTime.ParseExact(metadata.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
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
