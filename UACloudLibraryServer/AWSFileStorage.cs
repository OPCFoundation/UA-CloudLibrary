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
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Opc.Ua.Cloud.Library.Interfaces;

    /// <summary>
    /// AWS S3 storage class
    /// </summary>
    public class AWSFileStorage : IFileStorage
    {
        private readonly string _bucket;
        private readonly string _prefix;

        private readonly IAmazonS3 _s3Client;
        private readonly ILogger _logger;

        /// <summary>
        /// Default constructor
        /// </summary>
        public AWSFileStorage(IAmazonS3 s3Client, IConfiguration config, ILoggerFactory logger)
        {
            _s3Client = s3Client;
            _logger = logger.CreateLogger("AWSFileStorage");
            var connStr = config["BlobStorageConnectionString"];
            if (connStr != null)
            {
                try
                {
                    var uri = new AmazonS3Uri(connStr);

                    _bucket = uri.Bucket;
                    _prefix = uri.Key;
                }
                catch (Exception ex1)
                {
                    _logger.LogError($"{connStr} is not a valid S3 Url: {ex1.Message}");
                }
            }
            else
            {
                _logger.LogError($"S3 Url <BlobStorageConnectionString> not provided for file storage");
                _bucket = string.Empty;
                _prefix = string.Empty;
            }
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(_bucket))
            {
                _logger.LogError($"Error finding file {name} - S3 Bucket not specified");
                return null;
            }

            try
            {
                var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                await _s3Client.GetObjectMetadataAsync(_bucket, key, cancellationToken).ConfigureAwait(false);

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
                _logger.LogError($"Error updating file {name} - S3 Bucket not specified");
                return null;
            }

            try
            {
                var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

                var putRequest = new PutObjectRequest {
                    BucketName = _bucket,
                    Key = key,
                    InputStream = ms
                };

                var response = await _s3Client.PutObjectAsync(putRequest, cancellationToken).ConfigureAwait(false);
                if (response.HttpStatusCode == HttpStatusCode.OK)
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
                _logger.LogError($"Error downloading file {name} - S3 Bucket not specified");
                return null;
            }

            try
            {
                var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                var req = new GetObjectRequest {
                    BucketName = _bucket,
                    Key = key
                };

                var res = await _s3Client.GetObjectAsync(req, cancellationToken).ConfigureAwait(false);

                using (var reader = new StreamReader(res.ResponseStream))
                {
                    return reader.ReadToEnd();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download file {name}, {_s3Client.Config.ServiceURL}");

                return string.Empty;
            }
        }
        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task DeleteFileAsync(string name, CancellationToken cancellationToken = default)
        {
#if DEBUG
            if (string.IsNullOrEmpty(_bucket))
            {
                _logger.LogError($"Error deleting file {name} - S3 Bucket not specified");
                return;
            }

            try
            {
                var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                await _s3Client.DeleteObjectAsync(_bucket, key, cancellationToken).ConfigureAwait(false);

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {name}");
                return;
            }
#else
            await Task.CompletedTask;
#endif
        }
    }
}
