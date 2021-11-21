#nullable disable

namespace UACloudLibrary.DbContextModels
{
    public partial class Referencetype
    {
        public int ReferencetypeId { get; set; }
        public int? NodesetId { get; set; }
        public string ReferencetypeBrowsename { get; set; }
        public string ReferencetypeDisplayname { get; set; }
        public string ReferencetypeNamespace { get; set; }
    }
}
