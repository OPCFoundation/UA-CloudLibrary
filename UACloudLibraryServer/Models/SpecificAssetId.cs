
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AdminShell
{
    [Table("PersistSpecificAssetIds")]
    public partial class PersistedSpecificAssetIds
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        public virtual string AASId { get; set; }

        public virtual SpecificAssetId AssetId { get; set; }
    }

    [DataContract]
    public class SpecificAssetId : HasSemantics
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public virtual int Id { get; set; }

        /// <summary>
        /// Gets or Sets Name
        /// </summary>
        [Required]
        [RegularExpression("^([\\x09\\x0a\\x0d\\x20-\\ud7ff\\ue000-\\ufffd]|\\ud800[\\udc00-\\udfff]|[\\ud801-\\udbfe][\\udc00-\\udfff]|\\udbff[\\udc00-\\udfff])*$")]
        [StringLength(64, MinimumLength = 1)]
        [DataMember(Name = "name")]
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or Sets Value
        /// </summary>
        [Required]
        [RegularExpression("^([\\x09\\x0a\\x0d\\x20-\\ud7ff\\ue000-\\ufffd]|\\ud800[\\udc00-\\udfff]|[\\ud801-\\udbfe][\\udc00-\\udfff]|\\udbff[\\udc00-\\udfff])*$")]
        [StringLength(2048, MinimumLength = 1)]
        [DataMember(Name = "value")]
        public virtual string Value { get; set; }

        /// <summary>
        /// Gets or Sets ExternalSubjectId
        /// </summary>

        [DataMember(Name = "externalSubjectId")]
        public virtual Reference ExternalSubjectId { get; set; }
    }
}
