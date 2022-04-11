/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace SampleConsoleClient
{
    using System.Collections.Generic;

    public class UACloudLibGraphQLMetadataQueryResponse
    {
        public List<MetaData> metadata { get; set; }

        public class MetaData
        {
            public string metadata_name { get; set; }

            public string metadata_value { get; set; }

            public string nodeset_id { get; set; }
        }
    }
}
