using GraphQL;
using GraphQL.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UACloudLibrary;

namespace UA_CloudLibrary.GraphQL.Types
{
    public class OrganisationType : ObjectGraphType<Organisation>
    {
        public OrganisationType()
        {
            Name = "Organisations";
            Description = "Organistions that contributed an AddressSpace";
            Field(x => x.ID);
            Field(x => x.ContactEmail, nullable: true);
            Field(x => x.LogoUrl, nullable: true);
            Field(x => x.LastModification);
            Field(x => x.CreationTimeStamp);
            Field(x => x.Name);
            Field(x => x.Description, nullable: true);
            Field(x => x.Website, nullable: true);
        }
    }
}
