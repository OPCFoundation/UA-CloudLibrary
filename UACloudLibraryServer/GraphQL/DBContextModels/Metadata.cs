#nullable disable

namespace UACloudLibrary.DbContextModels
{
    public partial class Metadata
    {
        public int MetadataId { get; set; }

        public int? NodesetId { get; set; }

        public string MetadataName { get; set; }

        public string MetadataValue { get; set; }
    }
}
