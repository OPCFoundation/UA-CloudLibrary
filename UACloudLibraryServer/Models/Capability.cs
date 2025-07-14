
namespace AdminShell
{
    public class Capability : SubmodelElement
    {
        public Capability()
        {
            ModelType = ModelTypes.Capability;
        }

        public Capability(SubmodelElement src)
            : base(src)
        {
            ModelType = ModelTypes.Capability;
        }
    }
}
