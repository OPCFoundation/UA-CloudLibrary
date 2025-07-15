
namespace AdminShell
{
    public class SemanticId : Reference
    {
        public SemanticId() { }

        public SemanticId(SemanticId src)
        {
            if (src != null)
                foreach (var k in src.Keys)
                    Keys.Add(k);
        }
    }
}
