using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Opc.Ua.Export;

namespace Opc.Ua.Cloud.Library
{
    public static class NodeModelOpcExtensions
    {
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

        internal static void SetEngineeringUnits(this VariableModel model, EUInformation euInfo)
        {
            model.EngineeringUnit = new VariableModel.EngineeringUnitInfo {
                DisplayName = euInfo.DisplayName?.ToModelSingle(),
                Description = euInfo.Description?.ToModelSingle(),
                NamespaceUri = euInfo.NamespaceUri,
                UnitId = euInfo.UnitId,
            };
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
