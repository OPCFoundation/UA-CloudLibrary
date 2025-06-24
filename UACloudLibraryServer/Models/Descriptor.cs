
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AdminShell
{
    [DataContract]
    public partial class Descriptor
    {
        /// <summary>
        /// Gets or Sets Description
        /// </summary>
        [DataMember(Name="description")]
        public List<LangString> Description { get; set; }

        /// <summary>
        /// Gets or Sets DisplayName
        /// </summary>
        [DataMember(Name="displayName")]
        public List<LangString> DisplayName { get; set; }

        /// <summary>
        /// Gets or Sets Extensions
        /// </summary>
        [DataMember(Name="extensions")]
        public List<Extension> Extensions { get; set; }
    }
}
