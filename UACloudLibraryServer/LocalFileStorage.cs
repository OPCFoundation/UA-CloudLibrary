/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    /// <summary>
    /// Azure storage class
    /// </summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public LocalFileStorage(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger("LocalFileStorage");
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(Path.Combine(Path.GetTempPath(), name)))
                {
                    return Task.FromResult(name);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return the filename for storage in the index db
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), name), content).ConfigureAwait(false);
                return name;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Download a blob to a file.
        /// </summary>
        public async Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return await File.ReadAllTextAsync(Path.Combine(Path.GetTempPath(), name)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
    }
}
