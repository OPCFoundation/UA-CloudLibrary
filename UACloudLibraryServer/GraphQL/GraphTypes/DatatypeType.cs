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

    public class DatatypeType : ObjectGraphType<DatatypeModel>
    {
        public DatatypeType()
        {
            Field(a => a.datatype_id);
            Field(a => a.nodeset_id);
            Field(a => a.datatype_browsename);
            Field(a => a.datatype_value);
            Field(a => a.datatype_namespace);
        }
    }
}
