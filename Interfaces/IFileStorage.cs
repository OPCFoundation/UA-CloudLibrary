
namespace UA_CloudLibrary.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Azure storage interface
    /// </summary>
    public interface IFileStorage
    {
        /// <summary>
        /// Find nodesets based on certain keywords
        /// </summary>
        Task<string[]> FindFilesAsync(string keywords, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a nodeset from a local directory to storage
        /// </summary>
        Task<bool> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a nodeset from storage to a local file
        /// </summary>
        Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default);
    }
}
