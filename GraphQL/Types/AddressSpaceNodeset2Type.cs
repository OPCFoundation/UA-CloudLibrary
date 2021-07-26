using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.Types
{
    public class AddressSpaceNodeset2Type : ObjectGraphType<AddressSpaceNodeset2>
    {
        public AddressSpaceNodeset2Type()
        {
            Field(x => x.AddressSpaceID);
            Field(x => x.NodesetXml);
            Field(x => x.CreationTimeStamp);
            Field(x => x.LastModification);
        }
    }
}
