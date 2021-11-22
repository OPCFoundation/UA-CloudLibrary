
using System.ComponentModel.DataAnnotations;

namespace UACloudLibrary.DbContextModels
{
    public partial class VariabletypeModel
    {
        [Key]
        public int variabletype_id { get; set; }

        public long nodeset_id { get; set; }

        public string variabletype_browsename { get; set; }

        public string variabletype_value { get; set; }

        public string variabletype_namespace { get; set; }
    }
}
