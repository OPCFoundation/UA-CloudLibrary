using Opc.Ua.Cloud.Library;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Pins the commit-state contract that upload callers branch on when auditing an outcome.
    /// </summary>
    /// <remarks>
    /// An upload is not atomic: the nodeset blob is written, then its metadata, then the node-model
    /// index. A failure at either later stage leaves storage changed, and the caller cannot tell
    /// that from the message alone. Getting this wrong has two consequences - the audit trail
    /// records a durable change as never having happened, and a failure of that audit write is
    /// reported to the client as a safe retry.
    /// </remarks>
    public class UploadResultTests
    {
        [Fact]
        public void Success_IsSucceededAndCommitted()
        {
            UploadResult result = UploadResult.Success();

            Assert.True(result.Succeeded);
            Assert.True(result.StorageCommitted);
            Assert.False(result.PartiallyApplied);
            Assert.Equal(UploadResult.SuccessMessage, result.Message);
        }

        [Fact]
        public void Failed_IsNeitherSucceededNorCommitted()
        {
            // A pre-write failure: nothing was stored, so the caller records a plain Failed outcome
            // and retrying is safe.
            UploadResult result = UploadResult.Failed("nodeset already exists");

            Assert.False(result.Succeeded);
            Assert.False(result.StorageCommitted);
            Assert.False(result.PartiallyApplied);
            Assert.Equal("nodeset already exists", result.Message);
        }

        [Fact]
        public void FailedAfterStorageWrite_IsPartiallyApplied()
        {
            // The case both upload controllers branch on: the blob is durable but the operation did
            // not complete, so the outcome is PartialFailure recorded as a committed change.
            UploadResult result = UploadResult.FailedAfterStorageWrite("could not be indexed");

            Assert.False(result.Succeeded);
            Assert.True(result.StorageCommitted);
            Assert.True(result.PartiallyApplied);
            Assert.Equal("could not be indexed", result.Message);
        }

        [Fact]
        public void PartiallyApplied_IsFalseOnSuccess()
        {
            // Success also commits storage, so PartiallyApplied must not be derived from
            // StorageCommitted alone - otherwise a successful upload would be recorded as partial.
            Assert.False(UploadResult.Success().PartiallyApplied);
        }

        [Fact]
        public void SucceededMatchesOnlyTheExactSuccessMessage()
        {
            // Succeeded is a message comparison, so a failure whose text merely contains the word
            // must not read as success.
            Assert.False(UploadResult.Failed("upload was not a success").Succeeded);
            Assert.False(UploadResult.Failed("Success").Succeeded);
        }
    }
}
