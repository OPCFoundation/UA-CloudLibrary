
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class MetadataModel
    {
        [Key]
        public int metadata_id { get; set; }

        public long nodeset_id { get; set; }

        public string metadata_name { get; set; }

        public string metadata_value { get; set; }
    }
}
