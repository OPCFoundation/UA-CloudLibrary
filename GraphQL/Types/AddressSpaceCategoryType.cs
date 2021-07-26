using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.Types
{
    public class AddressSpaceCategoryType : ObjectGraphType<AddressSpaceCategory>
    {
        public AddressSpaceCategoryType()
        {
            Field(x => x.ID);
            Field(x => x.Name);
            Field(x => x.CreationTimeStamp);
            Field(x => x.LastModification);
            Field(x => x.IconUrl, nullable: true);
            Field(x => x.Description, nullable: true);
        }
    }
}
