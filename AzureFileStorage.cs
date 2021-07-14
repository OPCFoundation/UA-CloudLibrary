namespace UACloudLibrary
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    /// <summary>
    /// Azure storage class
    /// </summary>
    public class AzureFileStorage : IFileStorage
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public AzureFileStorage()
        {
            // nothing to do
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public async Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobStorageConnectionString")))
                {
                    // open blob storage
                    BlobContainerClient container = new BlobContainerClient(Environment.GetEnvironmentVariable("BlobStorageConnectionString"), "uacloudlib");
                    await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    var resultSegment = container.GetBlobsAsync();
                    await foreach (BlobItem blobItem in resultSegment.ConfigureAwait(false))
                    {
                        if (blobItem.Name == name)
                        {
                            return blobItem.Name;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob.
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobStorageConnectionString")))
                {
                    // open blob storage
                    BlobContainerClient container = new BlobContainerClient(Environment.GetEnvironmentVariable("BlobStorageConnectionString"), "uacloudlib");
                    await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    // Get a reference to the blob
                    BlobClient blob = container.GetBlobClient(name);

                    // Open the file and upload its data
                    using (MemoryStream file = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                    {
                        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        await blob.UploadAsync(file, cancellationToken).ConfigureAwait(false);

                        // Verify uploaded
                        BlobProperties properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                        if (file.Length != properties.ContentLength)
                        {
                            throw new Exception("Could not verify upload!");
                        }

                        return name;
                    }
                }

                return string.Empty;
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
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BlobStorageConnectionString")))
                {
                    // open blob storage
                    BlobContainerClient container = new BlobContainerClient(Environment.GetEnvironmentVariable("BlobStorageConnectionString"), "UACloudLib");
                    await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                    var resultSegment = container.GetBlobsAsync();
                    await foreach (BlobItem blobItem in resultSegment.ConfigureAwait(false))
                    {
                        if (blobItem.Name.Equals(name))
                        {
                            // Get a reference to the blob
                            BlobClient blob = container.GetBlobClient(blobItem.Name);

                            // Download the blob's contents and save it to a file
                            BlobDownloadInfo download = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
                            using (MemoryStream file = new MemoryStream())
                            {
                                await download.Content.CopyToAsync(file, cancellationToken).ConfigureAwait(false);

                                // Verify download
                                BlobProperties properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                                if (file.Length != properties.ContentLength)
                                {
                                    throw new Exception("Could not verify upload!");
                                }

                                return Encoding.UTF8.GetString(file.ToArray());
                            }
                        }
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return string.Empty;
            }
        }
    }
}
