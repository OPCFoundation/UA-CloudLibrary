
namespace SMIP
{
    using System.Collections.Generic;

    public class AssetHierarchy
    {
        public List<Place> places { get; set; }

        public class Place
        {
            public string id { get; set; }

            public string displayName { get; set; }

            public List<Equipment> equipment { get; set; }
        }

        public class Equipment
        {
            public string displayName { get; set; }

            public string id { get; set; }

            public List<Attribute> attributes { get; set; }
        }

        public class Attribute
        {
            public string displayName { get; set; }

            public string id { get; set; }

            public string dataType { get; set; }
        }
    }
}
