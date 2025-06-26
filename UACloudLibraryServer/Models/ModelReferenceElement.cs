
namespace AdminShell
{
    public class ModelReferenceElement : ReferenceElement
    {
        public ModelReferenceElement()
        {
            ModelType = ModelTypes.ModelReferenceElement;
        }

        public ModelReferenceElement(SubmodelElement src)
            : base(src)
        {
            if (!(src is ModelReferenceElement mre))
            {
                return;
            }

            ModelType = ModelTypes.ModelReferenceElement;

            if (mre.Value != null)
            {
                Value = new Reference(mre.Value);
            }
        }
    }
}
