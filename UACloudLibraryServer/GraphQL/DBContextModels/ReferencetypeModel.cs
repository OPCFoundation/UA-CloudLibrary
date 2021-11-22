
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class ReferencetypeModel
    {
        [Key]
        public int referencetype_id { get; set; }

        public long nodeset_id { get; set; }

        public string referencetype_browsename { get; set; }

        public string referencetype_value { get; set; }

        public string referencetype_namespace { get; set; }
    }
}
