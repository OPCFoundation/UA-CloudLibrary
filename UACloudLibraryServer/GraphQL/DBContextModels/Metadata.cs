
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class Metadata
    {
        [Key]
        public int Metadata_id { get; set; }

        public long Nodeset_id { get; set; }

        public string Metadata_Name { get; set; }

        public string Metadata_Value { get; set; }
    }
}
