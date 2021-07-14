namespace UACloudLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using UACloudLibrary.Interfaces;

    /// <summary>
    /// Azure storage class
    /// </summary>
    public class LocalFileStorage : IFileStorage
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public LocalFileStorage()
        {
            // nothing to do
        }

        /// <summary>
        /// Find a files based on certain keywords
        /// </summary>
        public Task<string[]> FindFilesAsync(string keywords, CancellationToken cancellationToken = default)
        {
            try
            {
                List<string> results = new List<string>();

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
        /// Upload a file to a blob and return the filename for storage in the index db
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                var tempFile = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempFile, content);
                return tempFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File upload failed!");
                return null;
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
