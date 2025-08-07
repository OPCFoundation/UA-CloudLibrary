
namespace AdminShell
{
    [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, AllowMultiple = true)]
    public class MetaModelNameAttribute : System.Attribute
    {
        private string name;

        public MetaModelNameAttribute(string name)
        {
            this.name = name;
        }
    }
}
