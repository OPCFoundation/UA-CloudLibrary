using System.Collections.Generic;
using System.Linq;
using CESMII.OpcUa.NodeSetModel;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Opc.Ua.Cloud.Library
{
    public partial class QueryModel
    {
        [UseFiltering, UseSorting]
        public List<TypeStats> GetTypeUsageStats([Service(ServiceKind.Synchronized)] IDatabase dp)
        {
            return (dp as CloudLibDataProvider).GetTypeUsageStats();
        }
    }

    public class TypeStats
    {
        public string Namespace { get; set; }
        public string Name { get; set; }
        public string NodeClass { get; set; }
        public int SubTypeCount { get; set; }
        public int SubTypeExternalCount { get; set; }
        public int ComponentCount { get; set; }
        public int ComponentExternalCount { get; set; }
        public IEnumerable<string> NodeSetsExternal { get; set; }
        public int NodeSetsExternalCount => NodeSetsExternal.Count();
    }
    public partial class CloudLibDataProvider
    {
        public /*Dictionary<string, */List<TypeStats> GetTypeUsageStats()
        {
            var objectTypesInObjectsStats = _dbContext.nodeSets
                .SelectMany(nm => nm.Objects/*.Where(o => o.NodeSet.ModelUri != o.TypeDefinition.NodeSet.ModelUri)*/
                .Select(om => new { ObjectType = om.TypeDefinition, Namespace = om.NodeSet.ModelUri, BrowseName = om.BrowseName }))
                .Distinct()
                .ToList()
                .GroupBy(a => a.ObjectType.BrowseName, (key, a) =>
                    a.Select(a => new TypeStats {
                        Namespace = a.ObjectType.NodeSet.ModelUri,
                        Name = key,
                        NodeClass = nameof(ObjectTypeModel),
                        ComponentCount = 1,
                        ComponentExternalCount = a.ObjectType.NodeSet.ModelUri != a.Namespace ? 1 : 0,
                        NodeSetsExternal = new List<string> { a.Namespace }
                    })
                    .Aggregate((ts1, ts2) => new TypeStats {
                        Name = ts1.Name,
                        Namespace = ts1.Namespace,
                        NodeClass = ts1.NodeClass,
                        SubTypeCount = ts1.SubTypeCount + ts2.SubTypeCount,
                        SubTypeExternalCount = ts1.SubTypeExternalCount + ts2.SubTypeExternalCount,
                        ComponentCount = ts1.ComponentCount + ts2.ComponentCount,
                        ComponentExternalCount = ts1.ComponentExternalCount + ts2.ComponentExternalCount,
                        NodeSetsExternal = ts1.NodeSetsExternal.Union(ts2.NodeSetsExternal)
                    }))
                ;

            var dataTypeInVariablesStats = _dbContext.nodeSets
                .SelectMany(nm => nm.Properties/*.Where(p => p.NodeSet.ModelUri != p.TypeDefinition.NodeSet.ModelUri)*/
                .Select(p => new { DataType = p.DataType, Namespace = p.NodeSet.ModelUri, BrowseName = p.BrowseName }))
                .Distinct()
                .ToList()
                .Concat(
                    _dbContext.nodeSets
                    .SelectMany(nm => nm.DataVariables/*.Where(dv => dv.NodeSet.ModelUri != dv.TypeDefinition.NodeSet.ModelUri)*/
                    .Select(dv => new { DataType = dv.DataType, Namespace = dv.NodeSet.ModelUri, BrowseName = dv.BrowseName }))
                    .Distinct()
                    .ToList()
                    )
                .GroupBy(a => a.DataType.BrowseName, (key, a) =>
                    a.Select(a => new TypeStats {
                        Namespace = a.DataType.NodeSet.ModelUri,
                        Name = key,
                        NodeClass = nameof(DataTypeModel),
                        ComponentCount = 1,
                        ComponentExternalCount = a.DataType.NodeSet.ModelUri != a.Namespace ? 1 : 0,
                        NodeSetsExternal = new List<string> { a.Namespace }
                    })
                    .Aggregate((ts1, ts2) => new TypeStats {
                        Name = ts1.Name,
                        Namespace = ts1.Namespace,
                        NodeClass = ts1.NodeClass,
                        SubTypeCount = ts1.SubTypeCount + ts2.SubTypeCount,
                        SubTypeExternalCount = ts1.SubTypeExternalCount + ts2.SubTypeExternalCount,
                        ComponentCount = ts1.ComponentCount + ts2.ComponentCount,
                        ComponentExternalCount = ts1.ComponentExternalCount + ts2.ComponentExternalCount,
                        NodeSetsExternal = ts1.NodeSetsExternal.Union(ts2.NodeSetsExternal)
                    }))
                ;

            var dataTypeInStructsStats = _dbContext.nodeSets
                .SelectMany(nm => nm.DataTypes.SelectMany(dt => dt.StructureFields.Select(sf => new { StructureFieldDT = sf.DataType, ReferencingNamespace = dt.NodeSet.ModelUri, ReferencingBrowseName = dt.BrowseName }))/*.Where(o => o.NodeSet.ModelUri != o.TypeDefinition.NodeSet.ModelUri)*/
                .Select(sf => new { DataType = sf.StructureFieldDT, Namespace = sf.ReferencingNamespace, BrowseName = sf.ReferencingBrowseName }))
                .Distinct()
                .ToList()
                .GroupBy(a => a.DataType.BrowseName, (key, a) =>
                    a.Select(a => new TypeStats {
                        Namespace = a.DataType.NodeSet.ModelUri,
                        Name = key,
                        NodeClass = nameof(DataTypeModel),
                        ComponentCount = 1,
                        ComponentExternalCount = a.DataType.NodeSet.ModelUri != a.Namespace ? 1 : 0,
                        NodeSetsExternal = new List<string> { a.Namespace }
                    })
                    .Aggregate((ts1, ts2) => new TypeStats {
                        Name = ts1.Name,
                        Namespace = ts1.Namespace,
                        NodeClass = ts1.NodeClass,
                        SubTypeCount = ts1.SubTypeCount + ts2.SubTypeCount,
                        SubTypeExternalCount = ts1.SubTypeExternalCount + ts2.SubTypeExternalCount,
                        ComponentCount = ts1.ComponentCount + ts2.ComponentCount,
                        ComponentExternalCount = ts1.ComponentExternalCount + ts2.ComponentExternalCount,
                        NodeSetsExternal = ts1.NodeSetsExternal.Union(ts2.NodeSetsExternal)
                    }))
                ;

            var objectTypeSubTypeStats = _dbContext.nodeSets
                .SelectMany(n => n.ObjectTypes)
                .Where(o => o.SubTypes.Any())
                .Select(ot => new TypeStats {
                    Name = ot.BrowseName,
                    Namespace = ot.NodeSet.ModelUri,
                    NodeClass = nameof(ObjectTypeModel),
                    SubTypeCount = ot.SubTypes.Count,
                    SubTypeExternalCount = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Count(),
                    NodeSetsExternal = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Select(st => st.NodeSet.ModelUri).Distinct(),
                })
                .ToList();

            var variableTypeStats = _dbContext.nodeSets
                .SelectMany(n => n.VariableTypes)
                .Where(o => o.SubTypes.Any())
                .Select(ot => new TypeStats {
                    Name = ot.BrowseName,
                    Namespace = ot.NodeSet.ModelUri,
                    NodeClass = nameof(VariableTypeModel),
                    SubTypeCount = ot.SubTypes.Count,
                    SubTypeExternalCount = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Count(),
                    NodeSetsExternal = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Select(st => st.NodeSet.ModelUri).Distinct(),
                })
                .ToList();

            var dataTypeStats = _dbContext.nodeSets
                .SelectMany(n => n.DataTypes)
                .Where(o => o.SubTypes.Any())
                .Select(ot => new TypeStats {
                    Name = ot.BrowseName,
                    Namespace = ot.NodeSet.ModelUri,
                    NodeClass = nameof(DataTypeModel),
                    SubTypeCount = ot.SubTypes.Count,
                    SubTypeExternalCount = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Count(),
                    NodeSetsExternal = ot.SubTypes.Where(st => st.SuperType.NodeSet.ModelUri != st.NodeSet.ModelUri).Select(st => st.NodeSet.ModelUri).Distinct(),
                })
                .ToList();


            var combinedStats =
                objectTypesInObjectsStats
                .Concat(objectTypeSubTypeStats)
                .Concat(dataTypeStats)
                .Concat(dataTypeInVariablesStats)
                .Concat(dataTypeInStructsStats)
                .Concat(variableTypeStats)
                    //.GroupBy(ts => ts.Namespace)
                    //.ToDictionary(g => g.Key, g => g
                    .GroupBy(ts => ts.Name, (key, tsList) => tsList.Aggregate((ts1, ts2) => new TypeStats {
                        Name = ts1.Name,
                        Namespace = ts1.Namespace,
                        NodeClass = ts1.NodeClass,
                        SubTypeCount = ts1.SubTypeCount + ts2.SubTypeCount,
                        SubTypeExternalCount = ts1.SubTypeExternalCount + ts2.SubTypeExternalCount,
                        ComponentCount = ts1.ComponentCount + ts2.ComponentCount,
                        ComponentExternalCount = ts1.ComponentExternalCount + ts2.ComponentExternalCount,
                        NodeSetsExternal = ts1.NodeSetsExternal.Union(ts2.NodeSetsExternal)
                    }))
                    //.OrderByDescending(ts => ts.SubTypeExternalCount)
                    .ToList()
            ;

            return combinedStats;
        }

    }
}
