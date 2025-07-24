namespace BlobToPGTable
{
    using System.ComponentModel;
    using System.Text;
    using System.Threading;
    using Amazon;
    using Amazon.S3;
    using Amazon.S3.Model;
    using Amazon.S3.Util;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Google.Cloud.Storage.V1;
    using Npgsql;

    internal sealed class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: BlobToPGTable <HostingPlatform> <Blob Storage Connection String> <PostgreSQL Connection String> [<AWSRegion>]");
                return;
            }

            using var conn = new NpgsqlConnection(args[2]);
            conn.Open();

            if (args[0] == "Azure")
            {
                var blobServiceClient = new BlobServiceClient(args[1]);
                var container = blobServiceClient.GetBlobContainerClient("uacloudlib");
                Azure.AsyncPageable<BlobItem> resultSegment = container.GetBlobsAsync();

                await foreach (BlobItem blobItem in resultSegment.ConfigureAwait(false))
                {
                    var blobClient = container.GetBlobClient(blobItem.Name);
                    var downloadInfo = await blobClient.DownloadAsync().ConfigureAwait(false);

                    using (MemoryStream file = new MemoryStream())
                    {
                        await downloadInfo.Value.Content.CopyToAsync(file).ConfigureAwait(false);

                        string nodesetXml = Encoding.UTF8.GetString(file.ToArray());
                        await UpdateDatabase(conn, blobItem.Name, nodesetXml).ConfigureAwait(false);
                    }
                }
            }
            else if (args[0] == "AWS")
            {
                var region = RegionEndpoint.EnumerableAllRegions.FirstOrDefault(r => r.SystemName.Equals(args[3], StringComparison.OrdinalIgnoreCase));
                var s3Client = new AmazonS3Client(region);

                var uri = new AmazonS3Uri(args[1]);
                if (!await AmazonS3Util.DoesS3BucketExistV2Async(s3Client, uri.Bucket).ConfigureAwait(false))
                {
                    Console.WriteLine($"Bucket {uri.Bucket} does not exist.");
                    return;
                }

                ListObjectsV2Response response = await s3Client.ListObjectsV2Async(new ListObjectsV2Request(){ BucketName = uri.Bucket }).ConfigureAwait(false);
                foreach (S3Object entry in response.S3Objects)
                {
                    var req = new GetObjectRequest {
                        BucketName = uri.Bucket,
                        Key = uri.Key
                    };

                    GetObjectResponse res = await s3Client.GetObjectAsync(req).ConfigureAwait(false);
                    using (var reader = new StreamReader(res.ResponseStream))
                    {
                        string nodesetXml = reader.ReadToEnd();
                        await UpdateDatabase(conn, uri.Key, nodesetXml).ConfigureAwait(false);
                    }
                }
            }
            else if (args[0] == "GCP")
            {
                var gcsClient = StorageClient.Create();
                foreach (var obj in gcsClient.ListObjects(args[1], ""))
                {
                    using (MemoryStream file = new MemoryStream())
                    {
                        await gcsClient.DownloadObjectAsync(args[1], obj.Name, file).ConfigureAwait(false);
                        string nodesetXml = Encoding.UTF8.GetString(file.ToArray());
                        await UpdateDatabase(conn, obj.Name, nodesetXml).ConfigureAwait(false);
                    }
                }
            }
            else if (args[0] == "Local")
            {
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.xml");
                foreach (string filePath in files)
                {
                    string fileName = Path.GetFileName(filePath);
                    string nodesetXml = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);
                    await UpdateDatabase(conn, fileName, nodesetXml).ConfigureAwait(false);
                }
            }
            else
            {
                Console.WriteLine("Invalid hosting platform specified. Use 'Azure', 'AWS', 'GCP' or 'Local'.");
            }

            conn.Close();
        }

        static async Task UpdateDatabase(NpgsqlConnection conn, string name, string nodesetXml)
        {
            using (var cmd = new NpgsqlCommand("INSERT INTO \"public\".\"DbFiles\" (\"Name\", \"Blob\") VALUES (@blobName, @nodesetXml)", conn))
            {
                cmd.Parameters.AddWithValue("blobName", name);
                cmd.Parameters.AddWithValue("nodesetXml", nodesetXml);

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            // verify successful insertion by reading back the data
            using (var verifyCmd = new NpgsqlCommand("SELECT * FROM \"public\".\"DbFiles\" WHERE \"Name\" = @blobName", conn))
            {
                verifyCmd.Parameters.AddWithValue("blobName", name);

                using var reader = await verifyCmd.ExecuteReaderAsync().ConfigureAwait(false);
                if (reader == null || !reader.HasRows)
                {
                    Console.WriteLine($"Failed to insert {name} into the database.");
                    return;
                }

                reader.Read();
                string nameReturned = reader.GetString(0);
                string blobContent = reader.GetString(1);

                if (nameReturned != name || blobContent != nodesetXml)
                {
                    Console.WriteLine($"Data mismatch for {nameReturned}. Expected: {name}, {nodesetXml}. Found: {nameReturned}, {blobContent}.");
                    return;
                }
            }

            Console.WriteLine($"Nodeset file {name} successfully added to database.");
        }
    }
}
