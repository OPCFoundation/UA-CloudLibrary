
namespace UACloudLibrary.Interfaces
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IFileStorage
    {
        /// <summary>
        /// Find a file based on a unique name
        /// </summary>
        Task<string> FindFileAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Upload a nodeset from a local directory to storage
        /// </summary>
        Task<string> UploadFileAsync(string name, string content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Download a nodeset from storage to a local file
        /// </summary>
        Task<string> DownloadFileAsync(string name, CancellationToken cancellationToken = default);
    }
}
