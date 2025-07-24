namespace AzureBlobToPGTable
{
    using System.ComponentModel;
    using System.Text;
    using System.Threading;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Npgsql;

    internal sealed class Program
    {
        static async Task Main(string[] args)
        {
            var blobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=uacloudlib;AccountKey=;EndpointSuffix=core.windows.net");
            var container = blobServiceClient.GetBlobContainerClient("uacloudlib");
            Azure.AsyncPageable<BlobItem> resultSegment = container.GetBlobsAsync();

            using var conn = new NpgsqlConnection("Host=;Username=;Password=;Database=uacloudlib;SSL Mode=Require");
            conn.Open();

            await foreach (BlobItem blobItem in resultSegment.ConfigureAwait(false))
            {
                var blobClient = container.GetBlobClient(blobItem.Name);
                var downloadInfo = await blobClient.DownloadAsync().ConfigureAwait(false);

                using (MemoryStream file = new MemoryStream())
                {
                    await downloadInfo.Value.Content.CopyToAsync(file).ConfigureAwait(false);
                    string nodesetXml = Encoding.UTF8.GetString(file.ToArray());

                    using (var cmd = new NpgsqlCommand("INSERT INTO \"public\".\"DbFiles\" (\"Name\", \"Blob\") VALUES (@blobName, @nodesetXml)", conn))
                    {
                        cmd.Parameters.AddWithValue("blobName", blobItem.Name);
                        cmd.Parameters.AddWithValue("nodesetXml", nodesetXml);

                        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    }

                    // verify successful insertion by reading back the data
                    using (var verifyCmd = new NpgsqlCommand("SELECT * FROM \"public\".\"DbFiles\" WHERE \"Name\" = @blobName", conn))
                    {
                        verifyCmd.Parameters.AddWithValue("blobName", blobItem.Name);

                        using var reader = await verifyCmd.ExecuteReaderAsync().ConfigureAwait(false);
                        if (reader == null || !reader.HasRows)
                        {
                            Console.WriteLine($"Failed to insert {blobItem.Name} into the database.");
                            continue;
                        }

                        reader.Read();
                        string name = reader.GetString(0);
                        string blobContent = reader.GetString(1);

                        if (name != blobItem.Name || blobContent != nodesetXml)
                        {
                            Console.WriteLine($"Data mismatch for {blobItem.Name}. Expected: {blobItem.Name}, {nodesetXml}. Found: {name}, {blobContent}.");
                        }
                    }

                    Console.WriteLine($"Nodeset file {blobItem.Name} successfully added to database.");
                }
            }

            conn.Close();
        }
    }
}
