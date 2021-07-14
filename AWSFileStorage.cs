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
    public class AWSFileStorage : IFileStorage
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public AWSFileStorage()
        {
            // TODO!
        }

        /// <summary>
        /// Find a files based on certain keywords
        /// </summary>
        public Task<string[]> FindFilesAsync(string keywords, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return Task.FromResult(new string[1]);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return Task.FromResult(new string[1]);
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
