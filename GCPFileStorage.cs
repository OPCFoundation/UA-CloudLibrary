namespace UACloudLibrary
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    /// <summary>
    /// Azure storage class
    /// </summary>
    public class GCPFileStorage : IFileStorage
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public GCPFileStorage()
        {
            // TODO!
        }

        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        public Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return a handle to the file that can be stored in the index database
        /// </summary>
        public Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File upload failed!");
                return Task.FromResult(string.Empty);
            }
        }

        /// <summary>
        /// Download a blob to a file.
        /// </summary>
        public Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return Task.FromResult(string.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return Task.FromResult(string.Empty);
            }
        }
    }
}
