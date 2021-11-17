using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.GraphTypes
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
