using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua;
using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Interfaces;
using Opc.Ua.Cloud.Library.NodeSetIndex;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public static class NodeModelOpcExtensions
    {
        public static string GetDisplayNamePath(this InstanceModelBase model)
        {
            return model.GetDisplayNamePath(new List<NodeModel>());
        }

        public static DateTime GetNormalizedPublicationDate(this ModelTableEntry model)
        {
            return model.PublicationDateSpecified ? DateTime.SpecifyKind(model.PublicationDate, DateTimeKind.Utc) : default;
        }

        public static DateTime GetNormalizedPublicationDate(this DateTime? publicationDate)
        {
            return publicationDate != null ? DateTime.SpecifyKind(publicationDate.Value, DateTimeKind.Utc) : default;
        }

        public static string GetDisplayNamePath(this InstanceModelBase model, List<NodeModel> nodesVisited)
        {
            if (nodesVisited.Contains(model))
            {
                return "(cycle)";
            }
            nodesVisited.Add(model);
            if (model.Parent is InstanceModelBase parent)
            {
                return $"{parent.GetDisplayNamePath(nodesVisited)}.{model.DisplayName.FirstOrDefault()?.Text}";
            }
            return model.DisplayName.FirstOrDefault()?.Text;
        }

        public static string GetUnqualifiedBrowseName(this NodeModel _this)
        {
            var browseName = _this.GetBrowseName();
            var parts = browseName.Split(new[] { ';' }, 2);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return browseName;
        }

        public enum JsonValueType
        {
            /// <summary>
            /// JSON object
            /// </summary>
            Object,
            /// <summary>
            /// Scalar, to be quoted
            /// </summary>
            String,
            /// <summary>
            /// Scalar, not to be quoted
            /// </summary>
            Value
        }


        public static JsonValueType GetJsonValueType(this DataTypeModel dataType)
        {
            if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.String}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Int64}") // numeric, but encoded as string
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.UInt64}") // numeric, but encoded as string
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.DateTime}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.ByteString}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.String}")
                )
            {
                return JsonValueType.String;
            }
            if (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Boolean}")
                || (dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Number}")
                    && !dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Decimal}") // numeric, but encoded as Scale/Value object
                    )
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.StatusCode}")
                || dataType.HasBaseType($"nsu={Namespaces.OpcUa};{DataTypeIds.Enumeration}")
                )
            {
                return JsonValueType.Value;
            }
            return JsonValueType.Object;
        }

        internal static void SetEngineeringUnits(this VariableModel model, EUInformation euInfo)
        {
            model.EngineeringUnit = new VariableModel.EngineeringUnitInfo {
                DisplayName = euInfo.DisplayName?.ToModelSingle(),
                Description = euInfo.Description?.ToModelSingle(),
                NamespaceUri = euInfo.NamespaceUri,
                UnitId = euInfo.UnitId,
            };
        }

        internal static void SetRange(this VariableModel model, Opc.Ua.Range euRange)
        {
            model.MinValue = euRange.Low;
            model.MaxValue = euRange.High;
        }

        internal static void SetInstrumentRange(this VariableModel model, Opc.Ua.Range range)
        {
            model.InstrumentMinValue = range.Low;
            model.InstrumentMaxValue = range.High;
        }
    }
}
