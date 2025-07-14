
namespace AdminShell
{
    using System.Runtime.Serialization;

    [DataContract]
    public class SubmodelElementStruct : SubmodelElementCollection
    {
        public SubmodelElementStruct()
        {
            ModelType = ModelTypes.SubmodelElementStruct;
        }

        public SubmodelElementStruct(SubmodelElement src)
            : base(src)
        {
            ModelType = ModelTypes.SubmodelElementStruct;
        }
    }
}
