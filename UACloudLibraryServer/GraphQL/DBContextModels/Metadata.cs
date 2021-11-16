using System;
using System.Collections.Generic;

#nullable disable

namespace UA_CloudLibrary.DbContextModels
{
    public partial class Metadata
    {
        public int MetadataId { get; set; }
        public int? NodesetId { get; set; }
        public string MetadataName { get; set; }
        public string MetadataValue { get; set; }

        //public virtual Nodeset Nodeset { get; set; }
    }
}
