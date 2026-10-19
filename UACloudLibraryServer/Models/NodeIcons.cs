/* ========================================================================
 * Copyright (c) 2005-2021 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Opc.Ua.Client;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Resolves the icon concept for a browsed node and maps it to the Iconify name declared in
    /// wwwroot/images/icon-map.json ("nodeIcon" section). The concept keys are the shared contract
    /// between this server and the other applications; only the JSON file decides which glyph is used.
    /// </summary>
    public static class NodeIcons
    {
        public const string ObjectInstance = "object";
        public const string Folder = "folder";
        public const string Property = "property";
        public const string EventType = "eventType";
        public const string Condition = "condition";
        public const string DataVariable = "dataVariable";
        public const string Method = "method";
        public const string View = "view";
        public const string ObjectType = "objectType";
        public const string InterfaceType = "interfaceType";
        public const string VariableType = "variableType";
        public const string DataType = "dataType";
        public const string ReferenceType = "referenceType";
        public const string Unknown = "unknown";

        private static readonly Lazy<IReadOnlyDictionary<string, string>> _iconMap = new(LoadIconMap);

        /// <summary>
        /// Returns the Iconify icon name (e.g. "material-symbols:folder-outline-rounded") for a concept key,
        /// falling back to the 'unknown' glyph when the key is not in the map.
        /// </summary>
        public static string GetIconName(string concept)
        {
            IReadOnlyDictionary<string, string> map = _iconMap.Value;

            if (!string.IsNullOrEmpty(concept) && map.TryGetValue(concept, out string iconName))
            {
                return iconName;
            }

            return map.TryGetValue(Unknown, out string unknown) ? unknown : null;
        }

        /// <summary>
        /// Resolves the icon concept for a browsed node. Instances (Object, Variable) are refined by their
        /// TypeDefinition, so a FolderType instance gets 'folder' and a PropertyType instance gets 'property';
        /// everything else is decided by the NodeClass alone.
        /// </summary>
        public static async Task<string> ResolveAsync(NodeClass nodeClass, ExpandedNodeId typeDefinition, ISession session)
        {
            switch (nodeClass)
            {
                case NodeClass.Object:
                    if (await IsTypeOfAsync(typeDefinition, ObjectTypeIds.FolderType, session).ConfigureAwait(false))
                    {
                        return Folder;
                    }

                    if (await IsTypeOfAsync(typeDefinition, ObjectTypeIds.ConditionType, session).ConfigureAwait(false))
                    {
                        return Condition;
                    }

                    return ObjectInstance;

                case NodeClass.Variable:
                    return await IsTypeOfAsync(typeDefinition, VariableTypeIds.PropertyType, session).ConfigureAwait(false) ? Property : DataVariable;

                case NodeClass.Method:
                    return Method;

                case NodeClass.View:
                    return View;

                case NodeClass.ObjectType:
                    return ObjectType;

                case NodeClass.VariableType:
                    return VariableType;

                case NodeClass.DataType:
                    return DataType;

                case NodeClass.ReferenceType:
                    return ReferenceType;

                default:
                    return Unknown;
            }
        }

        private static async Task<bool> IsTypeOfAsync(ExpandedNodeId typeDefinition, NodeId superType, ISession session)
        {
            if (typeDefinition.IsNull)
            {
                return false;
            }

            if (typeDefinition == superType)
            {
                return true;
            }

            if (session?.NodeCache == null)
            {
                return false;
            }

            try
            {
                return await session.NodeCache.IsTypeOfAsync(typeDefinition, superType).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Dictionary<string, string> LoadIconMap()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                // The web root is the project folder when running from the IDE and the application
                // folder once published, so probe both before giving up.
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "icon-map.json");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "images", "icon-map.json");
                }

                if (!File.Exists(path))
                {
                    return result;
                }

                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty("nodeIcon", out JsonElement nodeIcons))
                {
                    foreach (JsonProperty property in nodeIcons.EnumerateObject())
                    {
                        if (property.Value.ValueKind == JsonValueKind.String)
                        {
                            result[property.Name] = property.Value.GetString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("NodeIcons: failed to load icon-map.json: " + ex.Message);
            }

            return result;
        }
    }
}
