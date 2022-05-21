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
    using global::Opc.Ua.Cloud.Library.Client.Models;

    static class MetadataConverter
    {
        /// <summary>
        /// Converts metadata to a list of combinedtypes, taking the nodeset id from the metadata as a combination point
        /// </summary>
        public static List<UANameSpace> Convert(List<MetadataResult> metadata)
        {
            Dictionary<string, UANameSpace> nameSpaces = new Dictionary<string, UANameSpace>();

            if (metadata != null)
            {
                foreach (MetadataResult item in metadata)
                {
                    string id = item.NodesetID.ToString(CultureInfo.InvariantCulture);
                    if (!nameSpaces.ContainsKey(id))
                    {
                        nameSpaces.Add(id, new UANameSpace());
                    }

                    ConvertCases(nameSpaces[id], item);
                }
            }

            return nameSpaces.Values.ToList();
        }

        public static List<UANameSpace> Convert(List<UANodesetResult> infos)
        {
            List<UANameSpace> result = new List<UANameSpace>();

            if (infos != null)
            {
                foreach (UANodesetResult info in infos)
                {
                    result.Add(Convert(info));
                }
            }

            return result;
        }

        public static UANameSpace Convert(UANodesetResult info)
        {
            UANameSpace nameSpace = new UANameSpace();

            nameSpace.Title = info.Title;
            nameSpace.Nodeset.Version = info.Version;
            nameSpace.Contributor.Name = info.Contributor;

            switch (info.License)
            {
                case "MIT":
                {
                    nameSpace.License = License.MIT;
                    break;
                }
                case "ApacheLicense20":
                {
                    nameSpace.License = License.ApacheLicense20;
                    break;
                }
                case "Custom":
                {
                    nameSpace.License = License.Custom;
                    break;
                }
                default:
                {
                    nameSpace.License = License.Custom;
                    break;
                }
            }
            nameSpace.Nodeset.PublicationDate = (info.CreationTime != null) ? info.CreationTime.Value : DateTime.MinValue;
            nameSpace.Nodeset.NamespaceUri = string.IsNullOrEmpty(info.NameSpaceUri) ? null : new Uri(info.NameSpaceUri);
            nameSpace.Nodeset.Identifier = info.Id;

            return nameSpace;
        }

        /// <summary>
        /// Converts with paging support so the UI dev doesn't have to deal with it
        /// </summary>
        public static List<UANameSpace> ConvertWithPaging(List<UANodesetResult> infos, int limit = 10, int offset = 0)
        {
            if (infos == null)
            {
                return null;
            }
            List<UANameSpace> result = new List<UANameSpace>();

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
        private static void ConvertCases(UANameSpace nameSpace, MetadataResult metadata)
        {
            switch (metadata.Name)
            {
                #region NameSpace Cases
                case "addressspacedescription":
                {
                    nameSpace.Description = metadata.Value;
                    break;
                }
                case "copyright":
                {
                    nameSpace.CopyrightText = metadata.Value;
                    break;
                }
                case "documentationurl":
                {
                    nameSpace.DocumentationUrl = new Uri(metadata.Value);
                    break;
                }
                case "licenseurl":
                {
                    nameSpace.LicenseUrl = new Uri(metadata.Value);
                    break;
                }
                case "purchasinginfo":
                {
                    nameSpace.PurchasingInformationUrl = new Uri(metadata.Value);
                    break;
                }
                case "keywords":
                {
                    nameSpace.Keywords = metadata.Value.Split(new char[] { ',' });
                    break;
                }
                case "locales":
                {
                    nameSpace.SupportedLocales = metadata.Value.Split(new char[] { ',' });
                    break;
                }
                case "numdownloads":
                {
                    nameSpace.NumberOfDownloads = System.Convert.ToUInt32(metadata.Value, CultureInfo.InvariantCulture);
                    break;
                }
                case "validationstatus":
                {
                    nameSpace.ValidationStatus = metadata.Value;
                    break;
                }
                case "addressspacename":
                {
                    nameSpace.Title = metadata.Value;
                    break;
                }
                case "license":
                {
                    switch (metadata.Value)
                    {
                        case "MIT":
                        {
                            nameSpace.License = License.MIT;
                            break;
                        }
                        case "ApacheLicense20":
                        {
                            nameSpace.License = License.ApacheLicense20;
                            break;
                        }
                        case "Custom":
                        {
                            nameSpace.License = License.Custom;
                            break;
                        }
                        default:
                        {
                            nameSpace.License = License.Custom;
                            break;
                        }
                    }
                    break;
                }
                case "version":
                {
                    nameSpace.Nodeset.Version = metadata.Value;
                    break;
                }
                case "releasenotes":
                {
                    nameSpace.ReleaseNotesUrl = new Uri(metadata.Value);
                    break;
                }
                case "testspecification":
                {
                    nameSpace.TestSpecificationUrl = new Uri(metadata.Value);
                    break;
                }
                #endregion

                #region Organistion Cases
                case "orgname":
                {
                    nameSpace.Contributor.Name = metadata.Value;
                    break;
                }
                case "orgdesciption":
                {
                    nameSpace.Contributor.Description = metadata.Value;
                    break;
                }
                case "orgcontact":
                {
                    nameSpace.Contributor.ContactEmail = metadata.Value;
                    break;
                }
                case "orgwebsite":
                {
                    nameSpace.Contributor.Website = new Uri(metadata.Value);
                    break;
                }
                case "orglogo":
                {
                    nameSpace.Contributor.LogoUrl = new Uri(metadata.Value);
                    break;
                }
                #endregion
                case "adressspacecreationtime":
                {
                    nameSpace.Nodeset.PublicationDate = DateTime.ParseExact(metadata.Value, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
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
