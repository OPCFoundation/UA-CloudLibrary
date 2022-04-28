/* Author:      Markus Horstmann, C-Labs
 * Last Update: 4/13/2022
 * License:     MIT
 * 
 * Some contributions thanks to CESMII – the Smart Manufacturing Institute, 2022
 */

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CESMII.OpcUa.NodeSetImporter
{
    public interface IUANodeSetResolver
    {
        Task<IEnumerable<string>> ResolveNodeSetsAsync(List<ModelNameAndVersion> missingModels);
    }
}
