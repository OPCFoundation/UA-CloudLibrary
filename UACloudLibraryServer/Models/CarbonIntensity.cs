
namespace AdminShell
{
    using System;

    public class CarbonIntensityQueryResult
    {
        public CarbonData[] data { get; set; }
    }

    public class CarbonData
    {
        public DateTime from { get; set; }

        public DateTime to { get; set; }

        public CarbonIntensity intensity { get; set; }
    }

    public class CarbonIntensity
    {
        public int forecast { get; set; }

        public int actual { get; set; }

        public string index { get; set; }
    }

    public class RegionQueryResult
    {
        public string region { get; set; }

        public string region_full_name { get; set; }

        public string signal_type { get; set; }
    }

    public class WattTimeQueryResult
    {
        public WattTimeQueryResultEntry[] data { get; set; }
    }

    public class WattTimeQueryResultEntry
    {
        public DateTime point_time { get; set; }

        public float value { get; set; }
    }
}