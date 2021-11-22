
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class Objecttype
    {
        [Key]
        public int Objecttype_id { get; set; }

        public long Nodeset_id { get; set; }

        public string Objecttype_BrowseName { get; set; }

        public string Objecttype_Value { get; set; }

        public string Objecttype_Namespace { get; set; }
    }
}
