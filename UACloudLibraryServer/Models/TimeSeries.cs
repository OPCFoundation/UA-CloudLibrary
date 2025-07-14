
namespace SMIP
{
    using System;
    using System.Collections.Generic;

    public class TimeSeries
    {
        public List<GetRawHistoryDataWithSampling> getRawHistoryDataWithSampling { get; set; }

        public class GetRawHistoryDataWithSampling
        {
            public string id { get; set; }

            public float? floatvalue { get; set; }

            public DateTime ts { get; set; }
        }
    }
}
