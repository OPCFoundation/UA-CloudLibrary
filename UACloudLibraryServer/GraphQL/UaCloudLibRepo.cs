using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UACloudLibrary.DbContextModels;

namespace UACloudLibrary
{
    public class UaCloudLibRepo
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
