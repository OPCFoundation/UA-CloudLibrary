
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class Identifiable : Referable
    {
        //[DataMember(Name = "administration")]
        //[XmlElement(ElementName = "administration")]
        //public AdministrativeInformation Administration { get; set; }

        //[DataMember(Name = "identification")]
        //[XmlElement(ElementName = "identification")]
        //public Identifier Identification { get; set; } = new Identifier();

        [DataMember(Name = "id")]
        [XmlElement(ElementName = "id")]
        public string Id { get; set; } = string.Empty;
    }
}
