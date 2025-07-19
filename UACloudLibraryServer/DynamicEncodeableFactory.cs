using System.Collections.Generic;
using System.Linq;

namespace Opc.Ua.Cloud.Library
{
    public class DynamicEncodeableFactory : EncodeableFactory
    {
        public DynamicEncodeableFactory(IEncodeableFactory factory) : base(factory)
        {
        }
    }
}
