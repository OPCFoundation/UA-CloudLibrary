/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using Google.Cloud.Storage.V1;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;


    /// <summary>
    /// GCP storage class
    /// </summary>
    public class GCPFileStorage : IFileStorage
    {
        private readonly string _bucket;
        private readonly StorageClient _gcsClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public GCPFileStorage(ILoggerFactory logger)
        {
            _gcsClient = StorageClient.Create();
            _logger = logger.CreateLogger("GCPFileStorage");
            _bucket = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
            if (_bucket == null)
            {
                _logger.LogError($"GCS Url <BlobStorageConnectionString> not provided for file storage");
                _bucket = string.Empty;
            }
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_bucket))
            {
                _logger.LogError($"Error finding file {name} - GCS Bucket not specified");
                return null;
            }

            try
            {
                await _gcsClient.GetObjectAsync(_bucket, name, cancellationToken: cancellationToken).ConfigureAwait(false);
                return name;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding file {name}");
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return a handle to the file that can be stored in the index database
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_bucket))
            {
                _logger.LogError($"Error updating file {name} - GCS Bucket not specified");
                return null;
            }

            try
            {
                var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

                var response = await _gcsClient.UploadObjectAsync(_bucket, name, "text/plain", ms, cancellationToken: cancellationToken).ConfigureAwait(false);

                if (response.Size > 0)
                {
                    return name;
                }
                else
                {
                    _logger.LogError($"File upload failed!");
                    return string.Empty;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload file {name}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Download a blob to a file.
        /// </summary>
        public async Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {

            if (string.IsNullOrEmpty(_bucket))
            {
                _logger.LogError($"Error downloading file {name} - GCS Bucket not specified");
                return null;
            }

            try
            {
                using MemoryStream file = new MemoryStream();
                await _gcsClient.DownloadObjectAsync(_bucket, name, file, cancellationToken: cancellationToken).ConfigureAwait(false);
                return Encoding.UTF8.GetString(file.ToArray());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download file {name}, {_bucket}");

                return string.Empty;
            }
        }
    }
}
