
namespace AdminShell
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class MetaModelName : System.Attribute
    {
        private string name;

        public MetaModelName(string name)
        {
            this.name = name;
        }
    }
}
