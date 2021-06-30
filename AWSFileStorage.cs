namespace UA_CloudLibrary
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using UA_CloudLibrary.Interfaces;

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
        /// Upload a file to a blob.
        /// </summary>
        public Task<bool> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                // TODO!

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File upload failed!");
                return Task.FromResult(false);
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
