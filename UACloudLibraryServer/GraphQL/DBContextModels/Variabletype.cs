#nullable disable

namespace UACloudLibrary.DbContextModels
{
    public partial class Variabletype
    {
        public int VariabletypeId { get; set; }
 
        public int? NodesetId { get; set; }
        
        public string VariabletypeBrowsename { get; set; }
        
        public string VariabletypeDisplayname { get; set; }
        
        public string VariabletypeNamespace { get; set; }
    }
}
