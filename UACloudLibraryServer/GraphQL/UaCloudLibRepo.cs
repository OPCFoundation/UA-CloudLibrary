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
    using Microsoft.EntityFrameworkCore;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using UACloudLibrary.DbContextModels;

    public class UaCloudLibRepo : ObjectGraphType
    {
        private AppDbContext _context;

        public UaCloudLibRepo(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<DatatypeModel>> GetDataTypes()
        {
            return _context.datatype.ToListAsync();
        }

        public Task<List<MetadataModel>> GetMetaData()
        {
            return _context.metadata.ToListAsync();
        }

        public Task<List<ObjecttypeModel>> GetObjectTypes()
        {
            return _context.objecttype.ToListAsync();
        }

        public Task<List<ReferencetypeModel>> GetReferenceTypes()
        {
            return _context.referencetype.ToListAsync();
        }

        public Task<List<VariabletypeModel>> GetVariableTypes()
        {
            return _context.variabletype.ToListAsync();
        }
    }
}
