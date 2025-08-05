using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Cloud.Library.NodeSetIndex
{
    public static class LocalizedTextExtensions
    {
        public static NodeModel.LocalizedText ToModelSingle(this LocalizedText text) =>
            text != null ? new NodeModel.LocalizedText { Text = text.Text, Locale = text.Locale } : null;

        public static List<NodeModel.LocalizedText> ToModel(this LocalizedText text) =>
            text != null ? new List<NodeModel.LocalizedText> { text.ToModelSingle() } : new List<NodeModel.LocalizedText>();

        public static List<NodeModel.LocalizedText> ToModel(this IEnumerable<LocalizedText> texts) =>
            texts?.Select(text => text.ToModelSingle()).Where(lt => lt != null).ToList();
    }
}
