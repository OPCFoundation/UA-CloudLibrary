/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IFileStorage
    {
        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a nodeset from a local directory to storage
        /// </summary>
        Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a nodeset from storage to a local file
        /// </summary>
        Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default);
    }
}
