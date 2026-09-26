namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Outcome of an upload, carrying whether anything was durably written.
    /// </summary>
    /// <remarks>
    /// An upload is not atomic: the nodeset blob is stored before its metadata is added, and the
    /// metadata step can still fail. The caller cannot tell that apart from a failure that changed
    /// nothing by looking at the message alone, but the difference matters twice over - the audit
    /// entry should record a partial write rather than a clean failure, and a subsequent audit
    /// failure must not be reported as a retryable refusal for an operation that did change storage.
    /// </remarks>
    public sealed class UploadResult
    {
        /// <summary>Message returned by a successful upload.</summary>
        public const string SuccessMessage = "success";

        private UploadResult(string message, bool storageCommitted)
        {
            Message = message;
            StorageCommitted = storageCommitted;
        }

        /// <summary>The status message: <see cref="SuccessMessage"/>, or a description of the failure.</summary>
        public string Message { get; }

        /// <summary>
        /// True when the nodeset blob was durably stored, whether or not the upload then succeeded.
        /// </summary>
        /// <remarks>
        /// Also true on success. Callers distinguish the partial case by combining this with
        /// <see cref="Succeeded"/>.
        /// </remarks>
        public bool StorageCommitted { get; }

        /// <summary>True when the whole upload completed.</summary>
        public bool Succeeded => string.Equals(Message, SuccessMessage, System.StringComparison.Ordinal);

        /// <summary>
        /// True when the upload failed *after* durably writing the nodeset blob, so storage changed
        /// but the operation did not complete.
        /// </summary>
        public bool PartiallyApplied => !Succeeded && StorageCommitted;

        public static UploadResult Success() => new(SuccessMessage, storageCommitted: true);

        /// <summary>A failure that left storage untouched; retrying it is safe.</summary>
        public static UploadResult Failed(string message) => new(message, storageCommitted: false);

        /// <summary>A failure that occurred after the nodeset blob was already written.</summary>
        public static UploadResult FailedAfterStorageWrite(string message) => new(message, storageCommitted: true);
    }
}
