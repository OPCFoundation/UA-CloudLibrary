
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
        /// Find nodesets based on certain keywords
        /// </summary>
        Task<string[]> FindNodesetsAsync(string keywords, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a nodeset from a local directory to storage
        /// </summary>
        Task<bool> UploadNodesetAsync(string name, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a nodeset from storage to a local file
        /// </summary>
        Task<string> DownloadNodesetAsync(string name, CancellationToken cancellationToken = default);
    }
}
