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

namespace Opc.Ua.Cloud.Library
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Google.Cloud.Storage.V1;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Cloud.Library.Interfaces;


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
