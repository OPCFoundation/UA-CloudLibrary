#nullable disable

namespace UACloudLibrary.DbContextModels
{
    public partial class Datatype
    {
        public int DatatypeId { get; set; }

        public int? NodesetId { get; set; }

        public string DatatypeBrowsename { get; set; }

        public string DatatypeDisplayname { get; set; }

        public string DatatypeNamespace { get; set; }
    }
}
