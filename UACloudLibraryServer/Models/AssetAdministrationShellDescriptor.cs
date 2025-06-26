
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace AdminShell
{
    [DataContract]
    public partial class AssetAdministrationShellDescriptor : Descriptor
    {
        /// <summary>
        /// Gets or Sets Administration
        /// </summary>

        [DataMember(Name = "administration")]
        public AdministrativeInformation Administration { get; set; }

        /// <summary>
        /// Gets or Sets AssetKind
        /// </summary>

        [DataMember(Name = "assetKind")]
        public AssetKind AssetKind { get; set; }

        /// <summary>
        /// Gets or Sets AssetType
        /// </summary>
        [StringLength(2048, MinimumLength = 1)]
        [DataMember(Name = "assetType")]
        public string AssetType { get; set; }

        /// <summary>
        /// Gets or Sets Endpoints
        /// </summary>

        [DataMember(Name = "endpoints")]
        public List<Endpoint> Endpoints { get; set; }

        /// <summary>
        /// Gets or Sets GlobalAssetId
        /// </summary>
        [RegularExpression("/^([\\x09\\x0a\\x0d\\x20-\\ud7ff\\ue000-\\ufffd]|\\ud800[\\udc00-\\udfff]|[\\ud801-\\udbfe][\\udc00-\\udfff]|\\udbff[\\udc00-\\udfff])*$/")]
        [StringLength(2048, MinimumLength = 1)]
        [DataMember(Name = "globalAssetId")]
        public string GlobalAssetId { get; set; }

        /// <summary>
        /// Gets or Sets IdShort
        /// </summary>
        [RegularExpression("/^[a-zA-Z][a-zA-Z0-9_-]*[a-zA-Z0-9_]+$/")]
        [StringLength(128, MinimumLength = 1)]
        [DataMember(Name = "idShort")]
        public string IdShort { get; set; }

        /// <summary>
        /// Gets or Sets Id
        /// </summary>
        [Required]
        [RegularExpression("/^([\\x09\\x0a\\x0d\\x20-\\ud7ff\\ue000-\\ufffd]|\\ud800[\\udc00-\\udfff]|[\\ud801-\\udbfe][\\udc00-\\udfff]|\\udbff[\\udc00-\\udfff])*$/")]
        [StringLength(2048, MinimumLength = 1)]
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or Sets SpecificAssetIds
        /// </summary>

        [DataMember(Name = "specificAssetIds")]
        public List<SpecificAssetId> SpecificAssetIds { get; set; }

        /// <summary>
        /// Gets or Sets SubmodelDescriptors
        /// </summary>

        [DataMember(Name = "submodelDescriptors")]
        public List<SubmodelDescriptor> SubmodelDescriptors { get; set; }
    }
}
