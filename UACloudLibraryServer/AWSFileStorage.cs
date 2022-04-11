/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

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

                var putRequest = new PutObjectRequest
                {
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

                var req = new GetObjectRequest
                {
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
    }
}
