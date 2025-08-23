/* ========================================================================
 * Copyright (c) 2005-2025 The OPC Foundation, Inc. All rights reserved.
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Controllers;
using Opc.Ua.Cloud.Library.Models;

namespace UANodesetWebViewer.Controllers
{
    [Authorize(Policy = "ApiPolicy")]
    public class UploadController : Controller
    {
        private readonly CloudLibDataProvider _database;

        public UploadController(CloudLibDataProvider database)
        {
            _database = database;
        }

        public ActionResult Index()
        {
            return View("Index", string.Empty);
        }

        [HttpPost]
        public async Task<IActionResult> UploadNodeset(
            IFormFile nodesetFile,
            IFormFile values,
            bool overwrite,
            string nodesettitle,
            string license,
            string copyright,
            string description,
            string documentationurl,
            string iconurl,
            string licenseurl,
            string keywords,
            string purchasinginfo,
            string releasenotes,
            string testspecification,
            string locales)
        {
            try
            {
                UANameSpace nameSpace = new UANameSpace();

                if ((nodesetFile == null) || (nodesetFile.Length == 0) || (nodesetFile.ContentType != "text/xml"))
                {
                    throw new ArgumentException("Invalid file specified!");
                }

                // file name validation
                FileInfo fileInfoNodeset = new(nodesetFile.FileName);

                using (MemoryStream stream = new())
                {
                    await nodesetFile.CopyToAsync(stream).ConfigureAwait(false);
                    nameSpace.Nodeset.NodesetXml = Encoding.UTF8.GetString(stream.GetBuffer());
                }

                string valuesContent = string.Empty;
                if ((values != null) && (values.Length > 0) && (values.ContentType == "text/json"))
                {
                    // file name validation
                    FileInfo fileInfoValues = new(values.FileName);

                    using (MemoryStream stream = new())
                    {
                        await values.CopyToAsync(stream).ConfigureAwait(false);
                        valuesContent = Encoding.UTF8.GetString(stream.GetBuffer());
                    }
                }

                if (!string.IsNullOrWhiteSpace(nodesettitle))
                {
                    nameSpace.Title = nodesettitle;
                }
                else
                {
                    throw new ArgumentException("Invalid nodeset title entered!");
                }

                if (!string.IsNullOrWhiteSpace(license))
                {
                    nameSpace.License = license;
                }
                else
                {
                    throw new ArgumentException("Invalid license text entered!");
                }

                if (!string.IsNullOrWhiteSpace(copyright))
                {
                    nameSpace.CopyrightText = copyright;
                }
                else
                {
                    throw new ArgumentException("Invalid copyright text entered!");
                }

                if (!string.IsNullOrWhiteSpace(description))
                {
                    nameSpace.Description = description;
                }
                else
                {
                    throw new ArgumentException("Invalid description entered!");
                }

                if (!string.IsNullOrWhiteSpace(documentationurl))
                {
                    nameSpace.DocumentationUrl = new Uri(documentationurl);
                }

                if (!string.IsNullOrWhiteSpace(iconurl))
                {
                    nameSpace.IconUrl = new Uri(iconurl);
                }

                if (!string.IsNullOrWhiteSpace(licenseurl))
                {
                    nameSpace.LicenseUrl = new Uri(licenseurl);
                }

                if (!string.IsNullOrWhiteSpace(keywords))
                {
                    nameSpace.Keywords = keywords.Split(',');
                }

                if (!string.IsNullOrWhiteSpace(purchasinginfo))
                {
                    nameSpace.PurchasingInformationUrl = new Uri(purchasinginfo);
                }

                if (!string.IsNullOrWhiteSpace(releasenotes))
                {
                    nameSpace.ReleaseNotesUrl = new Uri(releasenotes);
                }

                if (!string.IsNullOrWhiteSpace(testspecification))
                {
                    nameSpace.TestSpecificationUrl = new Uri(testspecification);
                }

                if (!string.IsNullOrWhiteSpace(locales))
                {
                    nameSpace.SupportedLocales = locales.Split(',');
                }

                string result = await _database.UploadNamespaceAndNodesetAsync(nameSpace, valuesContent, overwrite, User.Identity.Name).ConfigureAwait(false);

                return View("Index", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading nodeset: {ex.Message}");
                return View("Index", ex.Message);
            }
        }
    }
}
