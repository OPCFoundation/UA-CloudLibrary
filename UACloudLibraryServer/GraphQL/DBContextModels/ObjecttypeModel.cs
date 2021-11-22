
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class ObjecttypeModel
    {
        [Key]
        public int objecttype_id { get; set; }

        public long nodeset_id { get; set; }

        public string objecttype_browsename { get; set; }

        public string objecttype_value { get; set; }

        public string objecttype_namespace { get; set; }
    }
}
