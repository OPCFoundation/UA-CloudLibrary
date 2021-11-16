using System;
using System.Collections.Generic;

#nullable disable

namespace UA_CloudLibrary.DbContextModels
{
    public partial class Nodeset
    {
        public Nodeset()
        {
            Datatypes = new HashSet<Datatype>();
            Metadata = new HashSet<Metadata>();
            Objecttypes = new HashSet<Objecttype>();
            Referencetypes = new HashSet<Referencetype>();
            Variabletypes = new HashSet<Variabletype>();
        }

        public int NodesetId { get; set; }
        public string NodesetFilename { get; set; }

        public virtual ICollection<Datatype> Datatypes { get; set; }
        public virtual ICollection<Metadata> Metadata { get; set; }
        public virtual ICollection<Objecttype> Objecttypes { get; set; }
        public virtual ICollection<Referencetype> Referencetypes { get; set; }
        public virtual ICollection<Variabletype> Variabletypes { get; set; }
    }
}
