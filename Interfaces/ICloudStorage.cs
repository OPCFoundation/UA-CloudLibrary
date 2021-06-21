
namespace UA_CloudLibrary.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure storage interface
    /// </summary>
    public interface ICloudStorage
    {
        /// <summary>
        /// Find a files based on certain keywords
        /// </summary>
        Task<string[]> FindFilesAsync(string keywords, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a file with given extension from a local directory to cloud storage
        /// </summary>
        Task<bool> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a file with given extension from the cloud to a local file
        /// </summary>
        Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default);
    }
}
