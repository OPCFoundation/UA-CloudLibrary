using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;
using Opc.Ua.Cloud.Library.XRegistry;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// In-memory <see cref="IXRegistryNodesetStore"/> for controller tests. Records every write so
    /// tests can assert on what the controller asked the store to do.
    /// </summary>
    internal sealed class FakeXRegistryNodesetStore : IXRegistryNodesetStore
    {
        public const string DiUri = "http://opcfoundation.org/UA/DI/";
        public const string MachineryUri = "http://opcfoundation.org/UA/Machinery/";

        public List<UANameSpace> Nodesets { get; } = new();

        public Dictionary<uint, string> Xml { get; } = new();

        public List<uint> Deleted { get; } = new();

        public List<(string UserId, uint Id)> DeletedOwned { get; } = new();

        public List<uint> Downloads { get; } = new();

        public List<(string UserId, UANameSpace Namespace, bool Overwrite)> Uploads { get; } = new();

        public UploadResult NextUploadResult { get; set; } = UploadResult.Success();

        public Func<UANameSpace, uint?> IdentifierProvider { get; set; } = ns => 4242;

        public bool DeleteOwnedResult { get; set; } = true;

        public static FakeXRegistryNodesetStore WithSampleData()
        {
            FakeXRegistryNodesetStore store = new();
            store.Add(Sample(1001, DiUri, "1.03", new DateTime(2021, 3, 1, 0, 0, 0, DateTimeKind.Utc), "OPC UA for Devices"));
            store.Add(Sample(1002, DiUri, "1.04", new DateTime(2022, 11, 1, 0, 0, 0, DateTimeKind.Utc), "OPC UA for Devices"));
            store.Add(Sample(2001, MachineryUri, "1.02", new DateTime(2022, 5, 1, 0, 0, 0, DateTimeKind.Utc), "OPC UA for Machinery"));
            return store;
        }

        public static UANameSpace Sample(uint id, string uri, string version, DateTime publicationDate, string title)
        {
            return new UANameSpace {
                Title = title,
                Description = $"{title} {version}",
                License = "MIT",
                CopyrightText = "(c) OPC Foundation",
                Keywords = new[] { "test", "sample" },
                SupportedLocales = new[] { "en" },
                DocumentationUrl = new Uri("https://reference.opcfoundation.org/"),
                CreationTime = publicationDate.AddDays(1),
                NumberOfDownloads = 7,
                Nodeset = new Nodeset {
                    Identifier = id,
                    NamespaceUri = new Uri(uri),
                    Version = version,
                    PublicationDate = publicationDate,
                    LastModifiedDate = publicationDate,
                    RequiredModels = new List<RequiredModelInfo> {
                        new() { NamespaceUri = "http://opcfoundation.org/UA/", PublicationDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), Version = "1.04" }
                    }
                }
            };
        }

        public void Add(UANameSpace ns, string xml = null)
        {
            Nodesets.Add(ns);
            Xml[ns.Nodeset.Identifier] = xml ?? $"<UANodeSet><Models><Model ModelUri=\"{ns.Nodeset.NamespaceUri}\" /></Models></UANodeSet>";
        }

        public Task<UANameSpace[]> GetAllAsync(string userId, bool allNodesets)
            => Task.FromResult(Nodesets.ToArray());

        public Task<UANameSpace> GetAsync(string userId, uint identifier, bool allNodesets)
            => Task.FromResult(Nodesets.FirstOrDefault(n => n.Nodeset.Identifier == identifier));

        public Task<string> GetXmlAsync(uint identifier)
            => Task.FromResult(Xml.TryGetValue(identifier, out string xml) ? xml : null);

        public Task<UploadResult> UploadAsync(string userId, UANameSpace uaNamespace, bool overwrite)
        {
            Uploads.Add((userId, uaNamespace, overwrite));
            if (NextUploadResult.Succeeded)
            {
                uint id = IdentifierProvider(uaNamespace) ?? 0;
                uaNamespace.Nodeset.Identifier = id;
                Nodesets.RemoveAll(n => n.Nodeset.Identifier == id);
                Nodesets.Add(uaNamespace);
                Xml[id] = uaNamespace.Nodeset.NodesetXml;
            }
            return Task.FromResult(NextUploadResult);
        }

        public uint? GetIdentifier(UANameSpace uaNamespace) => IdentifierProvider(uaNamespace);

        public Task DeleteAsync(uint identifier)
        {
            Deleted.Add(identifier);
            Nodesets.RemoveAll(n => n.Nodeset.Identifier == identifier);
            return Task.CompletedTask;
        }

        public Task<bool> DeleteOwnedAsync(string userId, uint identifier)
        {
            DeletedOwned.Add((userId, identifier));
            if (DeleteOwnedResult)
            {
                Nodesets.RemoveAll(n => n.Nodeset.Identifier == identifier);
            }
            return Task.FromResult(DeleteOwnedResult);
        }

        public Task IncrementDownloadCountAsync(uint identifier)
        {
            Downloads.Add(identifier);
            return Task.CompletedTask;
        }
    }
}
