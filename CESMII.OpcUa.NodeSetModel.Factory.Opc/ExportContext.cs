using System.Collections.Generic;
using Opc.Ua.Export;
using CESMII.OpcUa.NodeSetModel.Factory.Opc;
using Microsoft.Extensions.Logging;

namespace CESMII.OpcUa.NodeSetModel.Export.Opc
{
    public class ExportContext : DefaultOpcUaContext
    {
        public ExportContext(ILogger logger, Dictionary<string, NodeSetModel> nodeSetModels) : base(nodeSetModels, logger)
        {
        }
        public Dictionary<string, string> Aliases;
        /// <summary>
        /// Assumes that any VariableModel.Value or VariableTypeModel.Value that contain scalars just contain the scalar value, rather than the OPC JSON encoding
        /// </summary>

        public HashSet<string> _nodeIdsUsed;
        public Dictionary<string, UANode> _exportedSoFar;
    }

}