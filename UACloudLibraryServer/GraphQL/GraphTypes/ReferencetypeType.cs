/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using GraphQL.Types;
    using UACloudLibrary.DbContextModels;

    public class ReferencetypeType : ObjectGraphType<ReferencetypeModel>
    {
        public ReferencetypeType()
        {
            Field(a => a.referencetype_id);
            Field(a => a.nodeset_id);
            Field(a => a.referencetype_browsename);
            Field(a => a.referencetype_value);
            Field(a => a.referencetype_namespace);
        }
    }
}
