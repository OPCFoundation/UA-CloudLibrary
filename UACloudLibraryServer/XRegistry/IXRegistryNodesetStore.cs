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

using System.Threading.Tasks;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library.XRegistry
{
    /// <summary>
    /// Data access used by the xRegistry API. Abstracts the Cloud Library database and blob storage
    /// so the controller can be unit tested without a database.
    /// </summary>
    public interface IXRegistryNodesetStore
    {
        /// <summary>All nodeset metadata (without XML) visible to the user, ordered by namespace URI then publication date.</summary>
        Task<UANameSpace[]> GetAllAsync(string userId, bool allNodesets);

        /// <summary>Metadata (without XML) visible to the user for one namespace, ordered by publication date.</summary>
        Task<UANameSpace[]> GetByNamespaceAsync(string userId, string namespaceUri, bool allNodesets);

        /// <summary>Metadata of a single nodeset, or null if it does not exist or is not visible to the user.</summary>
        Task<UANameSpace> GetAsync(string userId, uint identifier, bool allNodesets);

        /// <summary>The stored NodeSet2 XML of a nodeset, or null if it does not exist.</summary>
        Task<string> GetXmlAsync(uint identifier);

        /// <summary>Uploads or overwrites a nodeset. Uploads are always private; publication is a separate approval.</summary>
        Task<UploadResult> UploadAsync(string userId, UANameSpace uaNamespace, bool overwrite);

        /// <summary>Computes the identifier the library assigns to the nodeset XML, or null if the XML cannot be parsed.</summary>
        uint? GetIdentifier(UANameSpace uaNamespace);

        /// <summary>Deletes a nodeset regardless of owner. Only for administrators.</summary>
        Task DeleteAsync(uint identifier);

        /// <summary>Deletes a nodeset only if owned by the user. Returns false if not found or not owned.</summary>
        Task<bool> DeleteOwnedAsync(string userId, uint identifier);

        /// <summary>Counts a document download.</summary>
        Task IncrementDownloadCountAsync(uint identifier);
    }
}
