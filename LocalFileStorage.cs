namespace UACloudLibrary
{
    using System;
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
        /// Find a file based on a unique name
        /// </summary>
        public Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                if (File.Exists(Path.Combine(Path.GetTempPath(), name)))
                {
                    return Task.FromResult(name);
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return null;
            }
        }

        /// <summary>
        /// Upload a file to a blob and return the filename for storage in the index db
        /// </summary>
        public async Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default)
        {
            try
            {
                await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(), name), content);
                return name;
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
        public async Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                return await File.ReadAllTextAsync(Path.Combine(Path.GetTempPath(), name));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex, "File download failed!");
                return null;
            }
        }
    }
}
