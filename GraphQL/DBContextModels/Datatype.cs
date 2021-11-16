using System;
using System.Collections.Generic;

#nullable disable

namespace UA_CloudLibrary.DbContextModels
{
    public partial class Datatype
    {
        public int DatatypeId { get; set; }
        public int? NodesetId { get; set; }
        public string DatatypeBrowsename { get; set; }
        public string DatatypeDisplayname { get; set; }
        public string DatatypeNamespace { get; set; }

        //public virtual Nodeset Nodeset { get; set; }
    }
}
