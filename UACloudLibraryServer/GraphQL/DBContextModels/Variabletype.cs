using System;
using System.Collections.Generic;

#nullable disable

namespace UA_CloudLibrary.DbContextModels
{
    public partial class Variabletype
    {
        public int VariabletypeId { get; set; }
        public int? NodesetId { get; set; }
        public string VariabletypeBrowsename { get; set; }
        public string VariabletypeDisplayname { get; set; }
        public string VariabletypeNamespace { get; set; }

        //public virtual Nodeset Nodeset { get; set; }
    }
}
