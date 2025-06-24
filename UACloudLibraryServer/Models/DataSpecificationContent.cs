
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace AdminShell
{
    [DataContract]
    public class DataSpecificationContent
    {
        [DataMember(Name="dataSpecificationIEC61360")]
        [XmlElement(ElementName="dataSpecificationIEC61360")]
        public DataSpecificationIEC61360 DataSpecificationIEC61360 { get; set; } = new DataSpecificationIEC61360();

        public DataSpecificationContent() { }

        public DataSpecificationContent(DataSpecificationContent src)
        {
            if (src.DataSpecificationIEC61360 != null)
                DataSpecificationIEC61360 = new DataSpecificationIEC61360(src.DataSpecificationIEC61360);
        }
    }
}
