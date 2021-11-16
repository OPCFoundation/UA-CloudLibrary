namespace UACloudLibrary
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;
    using Amazon;
    using Amazon.Runtime;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using Amazon.SecurityToken;
    using Amazon.SecurityToken.Model;
    using System.IO;
    using System.Text;
    using System.Net;


    /// <summary>
    /// AWS S3 storage class
    /// </summary>
    public class AWSFileStorage : IFileStorage
    {
        private readonly string _bucket;
        private readonly string _prefix;
        private readonly RegionEndpoint _region;
        /// <summary>
        /// Default constructor
        /// </summary>
        public AWSFileStorage()
        {
            var connStr = Environment.GetEnvironmentVariable("BlobStorageConnectionString");
            if (connStr != null)
            {
                try
                {
                    var uri = new AmazonS3Uri(connStr);

                    _bucket = uri.Bucket;
                    _prefix = uri.Key;
                    if (uri.Region != null) _region = uri.Region;
                }
                catch (Exception ex1)
                {
                    Debug.WriteLine($"{connStr} is not a valid S3 Url: {ex1.Message}");
                }
            }

            if (_region == null)
            {
                var regionName = Environment.GetEnvironmentVariable("AWS_REGION");
                if (!string.IsNullOrEmpty(regionName))
                {
                    try
                    {
                        _region = RegionEndpoint.GetBySystemName(regionName);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine($"{regionName} is not a valid AWS region");
                    }
                }
            }
        }

        private async Task<AmazonS3Client> ConnectToS3(CancellationToken cancellationToken)
        {
            var cred = await GetTemporaryCredentialsAsync(cancellationToken);
            var config = _region == null ? new AmazonS3Config() : new AmazonS3Config { RegionEndpoint = _region };

            return new AmazonS3Client(cred, config);

        }

        private static async Task<AWSCredentials> GetTemporaryCredentialsAsync(CancellationToken cancellationToken)
        {
            Credentials credentials = null;

            var roleArn = Environment.GetEnvironmentVariable("AWSRoleArn");
            if (string.IsNullOrEmpty(roleArn))
            {
                return FallbackCredentialsFactory.GetCredentials();
            }

            using (var stsClient = new AmazonSecurityTokenServiceClient())
            {
                var request = new AssumeRoleRequest
                {
                    RoleArn = roleArn,
                    DurationSeconds = 1200,
                    RoleSessionName = "S3AccessRole"
                };

                var response = await stsClient.AssumeRoleAsync(request, cancellationToken);
                credentials = response.Credentials;
            }

            var sessionCredentials =
                        new SessionAWSCredentials(credentials.AccessKeyId,
                                                  credentials.SecretAccessKey,
                                                  credentials.SessionToken);
            return sessionCredentials;

        }
        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var s3Client = await ConnectToS3(cancellationToken))
                {
                    var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                    await s3Client.GetObjectMetadataAsync(_bucket, key, cancellationToken);

                }

                return name;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File not found!");
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return a handle to the file that can be stored in the index database
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var s3 = await ConnectToS3(cancellationToken))
                {
                    var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucket,
                        Key = key,
                        InputStream = ms
                    };

                    var response = await s3.PutObjectAsync(putRequest, cancellationToken);
                    if (response.HttpStatusCode == HttpStatusCode.OK)
                    {
                        return name;
                    }
                    else
                    {
                        Debug.WriteLine($"File upload failed!");
                        return string.Empty;
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File upload failed!");
                return string.Empty;
            }
        }

        /// <summary>
        /// Download a blob to a file.
        /// </summary>
        public async Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var s3 = await ConnectToS3(cancellationToken))
                {
                    var key = string.IsNullOrEmpty(_prefix) ? name : _prefix + name;

                    var req = new GetObjectRequest
                    {
                        BucketName = _bucket,
                        Key = key
                    };

                    var res = await s3.GetObjectAsync(req, cancellationToken);

                    using (var reader = new StreamReader(res.ResponseStream))
                    {
                        return reader.ReadToEnd();
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return string.Empty;
            }
        }
    }
}
