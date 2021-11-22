
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class DatatypeModel
    {
        [Key]
        public int datatype_id { get; set; }

        public long nodeset_id { get; set; }

        public string datatype_browsename { get; set; }

        public string datatype_value { get; set; }

        public string datatype_namespace { get; set; }
    }
}
