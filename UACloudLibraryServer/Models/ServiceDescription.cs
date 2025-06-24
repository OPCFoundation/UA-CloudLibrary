
namespace AdminShell
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// The Description object enables servers to present their capabilities to the clients, in particular which profiles they implement. At least one defined profile is required. Additional, proprietary attributes might be included. Nevertheless, the server must not expect that a regular client understands them.
    /// </summary>
    [DataContract]
    public partial class ServiceDescription
    {
        /// <summary>
        /// Gets or Sets Profiles
        /// </summary>
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public enum ProfilesEnum
        {
            /// <summary>
            /// Enum AssetAdministrationShellServiceSpecificationV30Enum for AssetAdministrationShellServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "AssetAdministrationShellServiceSpecification/V3.0")]
            AssetAdministrationShellServiceSpecificationV30Enum = 0,

            /// <summary>
            /// Enum AssetAdministrationShellServiceSpecificationV30MinimalProfileEnum for AssetAdministrationShellServiceSpecification/V3.0-MinimalProfile
            /// </summary>
            [EnumMember(Value = "AssetAdministrationShellServiceSpecification/V3.0-MinimalProfile")]
            AssetAdministrationShellServiceSpecificationV30MinimalProfileEnum = 1,

            /// <summary>
            /// Enum SubmodelServiceSpecificationV30Enum for SubmodelServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "SubmodelServiceSpecification/V3.0")]
            SubmodelServiceSpecificationV30Enum = 2,

            /// <summary>
            /// Enum SubmodelServiceSpecificationV30ValueProfileEnum for SubmodelServiceSpecification/V3.0-ValueProfile
            /// </summary>
            [EnumMember(Value = "SubmodelServiceSpecification/V3.0-ValueProfile")]
            SubmodelServiceSpecificationV30ValueProfileEnum = 3,
            /// <summary>
            /// Enum SubmodelServiceSpecificationV30MinimalProfileEnum for SubmodelServiceSpecification/V3.0-MinimalProfile
            /// </summary>
            [EnumMember(Value = "SubmodelServiceSpecification/V3.0-MinimalProfile")]
            SubmodelServiceSpecificationV30MinimalProfileEnum = 4,

            /// <summary>
            /// Enum AasxFileServerServiceSpecificationV30Enum for AasxFileServerServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "AasxFileServerServiceSpecification/V3.0")]
            AasxFileServerServiceSpecificationV30Enum = 5,

            /// <summary>
            /// Enum RegistryServiceSpecificationV30Enum for RegistryServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "RegistryServiceSpecification/V3.0")]
            RegistryServiceSpecificationV30Enum = 6,

            /// <summary>
            /// Enum RegistryServiceSpecificationV30AssetAdministrationShellRegistryEnum for RegistryServiceSpecification/V3.0- AssetAdministrationShellRegistry
            /// </summary>
            [EnumMember(Value = "RegistryServiceSpecification/V3.0- AssetAdministrationShellRegistry")]
            RegistryServiceSpecificationV30AssetAdministrationShellRegistryEnum = 7,

            /// <summary>
            /// Enum RegistryServiceSpecificationV30SubmodelRegistryEnum for RegistryServiceSpecification/V3.0-SubmodelRegistry
            /// </summary>
            [EnumMember(Value = "RegistryServiceSpecification/V3.0-SubmodelRegistry")]
            RegistryServiceSpecificationV30SubmodelRegistryEnum = 8,

            /// <summary>
            /// Enum RepositoryServiceSpecificationV30Enum for RepositoryServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "RepositoryServiceSpecification/V3.0")]
            RepositoryServiceSpecificationV30Enum = 9,

            /// <summary>
            /// Enum RepositoryServiceSpecificationV30MinimalProfileEnum for RepositoryServiceSpecification/V3.0-MinimalProfile
            /// </summary>
            [EnumMember(Value = "RepositoryServiceSpecification/V3.0-MinimalProfile")]
            RepositoryServiceSpecificationV30MinimalProfileEnum = 10,

            /// <summary>
            /// Enum AssetAdministrationShellRepositoryServiceSpecificationV30Enum for AssetAdministrationShellRepositoryServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "AssetAdministrationShellRepositoryServiceSpecification/V3.0")]
            AssetAdministrationShellRepositoryServiceSpecificationV30Enum = 11,

            /// <summary>
            /// Enum AssetAdministrationShellRepositoryServiceSpecificationV30MinimalProfileEnum for AssetAdministrationShellRepositoryServiceSpecification/V3.0-MinimalProfile
            /// </summary>
            [EnumMember(Value = "AssetAdministrationShellRepositoryServiceSpecification/V3.0-MinimalProfile")]
            AssetAdministrationShellRepositoryServiceSpecificationV30MinimalProfileEnum = 12,

            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationV30Enum for SubmodelRepositoryServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "SubmodelRepositoryServiceSpecification/V3.0")]
            SubmodelRepositoryServiceSpecificationV30Enum = 13,

            /// <summary>
            /// Enum SubmodelRepositoryServiceSpecificationV30MinimalProfileEnum for SubmodelRepositoryServiceSpecification/V3.0-MinimalProfile
            /// </summary>
            [EnumMember(Value = "SubmodelRepositoryServiceSpecification/V3.0-MinimalProfile")]
            SubmodelRepositoryServiceSpecificationV30MinimalProfileEnum = 14,

            /// <summary>
            /// Enum RegistryAndDiscoveryServiceSpecificationV30Enum for RegistryAndDiscoveryServiceSpecification/V3.0
            /// </summary>
            [EnumMember(Value = "RegistryAndDiscoveryServiceSpecification/V3.0")]
            RegistryAndDiscoveryServiceSpecificationV30Enum = 15
        }

        [DataMember(Name="profiles")]
        public List<ProfilesEnum> Profiles { get; set; }
    }
}
