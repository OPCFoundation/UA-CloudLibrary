
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class AdministrativeInformation
    {
        public AdministrativeInformation() { }

        public AdministrativeInformation(AdministrativeInformation administration)
        {
            Revision = administration.Revision;
            Version = administration.Version;
        }

        [DataMember(Name="revision")]
        [XmlElement(ElementName= "revision")]
        public string Revision { get; set; }

        [DataMember(Name="version")]
        [XmlElement(ElementName = "version")]
        public string Version { get; set; }
    }
}
