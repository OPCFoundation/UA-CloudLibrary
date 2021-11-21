using GraphQL.Types;

namespace UACloudLibrary
{
    public class AddressSpaceLicenseType : EnumerationGraphType
    {
        public AddressSpaceLicenseType()
        {
            AddValue("MIT", "", 0);
            AddValue("ApacheLicense20", "", 1);
            AddValue("Custom", "", 2);
        }
    }
}
