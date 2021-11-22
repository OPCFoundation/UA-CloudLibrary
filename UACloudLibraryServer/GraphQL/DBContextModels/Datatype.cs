
namespace UACloudLibrary.DbContextModels
{
    public partial class Datatype
    {
        public int Datatype_id { get; set; }

        public long Nodeset_id { get; set; }

        public string Datatype_BrowseName { get; set; }

        public string Datatype_Value { get; set; }

        public string Datatype_Namespace { get; set; }
    }
}
