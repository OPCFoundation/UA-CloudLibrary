
namespace AdminShell
{
    using System.Runtime.Serialization;
    using System.Xml.Serialization;

    [DataContract]
    public class GlobalReferenceElement : ReferenceElement
    {
        [DataMember(Name = "grvalue")]
        [XmlElement(ElementName = "grvalue")]
        public GlobalReference GRValue = new GlobalReference();

        public GlobalReferenceElement() { }

        public GlobalReferenceElement(SubmodelElement src)
            : base(src)
        {
            if (!(src is GlobalReferenceElement gre))
                return;

            if (gre.Value != null)
                this.Value = new GlobalReference(gre.Value);
        }
    }
}

