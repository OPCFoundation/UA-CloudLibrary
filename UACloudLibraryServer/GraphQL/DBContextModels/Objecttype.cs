#nullable disable

namespace UACloudLibrary.DbContextModels
{
    public partial class Objecttype
    {
        public int ObjecttypeId { get; set; }
        public int? NodesetId { get; set; }
        public string ObjecttypeBrowsename { get; set; }
        public string ObjecttypeDisplayname { get; set; }
        public string ObjecttypeNamespace { get; set; }
    }
}
