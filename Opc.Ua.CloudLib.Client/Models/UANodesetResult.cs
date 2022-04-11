/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace Opc.Ua.CloudLib.Client
{
    using Newtonsoft.Json;
    /// <summary>GraphQL Result for nodeset queries</summary>
    public class UANodesetResult
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <value>The identifier.</value>
        [JsonProperty(PropertyName = "nodeset_id")]
        public uint Id { get; set; }

        /// <summary>Gets or sets the title.</summary>
        /// <value>The title.</value>
        [JsonProperty(PropertyName = "nodesettitle")]
        public string Title { get; set; }

        /// <summary>Gets or sets the contributor.</summary>
        /// <value>The contributor.</value>
        [JsonProperty(PropertyName = "orgname")]
        public string Contributor { get; set; }

        /// <summary>Gets or sets the license.</summary>
        /// <value>The license.</value>
        [JsonProperty(PropertyName = "license")]
        public string License { get; set; }

        /// <summary>Gets or sets the version.</summary>
        /// <value>The version.</value>
        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        /// <summary>Gets or sets the creation time.</summary>
        /// <value>The creation time.</value>
        [JsonProperty(PropertyName = "adressspacecreationtime")]
        public System.DateTime? CreationTime { get; set; }    
    }
}
