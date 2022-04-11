/* Copyright (c) 1996-2022 The OPC Foundation. All rights reserved.
   The source code in this file is covered under a dual-license scenario:
     - RCL: for OPC Foundation Corporate Members in good-standing
     - GPL V2: everybody else
   RCL license terms accompanied with this source code. See http://opcfoundation.org/License/RCL/1.00/
   GNU General Public License as published by the Free Software Foundation;
*/

namespace UACloudLibrary
{
    using UACloudLibrary.Models;

    public interface IDatabase
    {
        UANodesetResult[] FindNodesets(string[] keywords);

        bool AddMetaDataToNodeSet(uint nodesetId, string name, string value);

        bool UpdateMetaDataForNodeSet(uint nodesetId, string name, string value);

        bool AddUATypeToNodeset(uint nodesetId, UATypes uaType, string browseName, string displayName, string nameSpace);

        bool DeleteAllRecordsForNodeset(uint nodesetId);

        string RetrieveMetaData(uint nodesetId, string metaDataTag);

        string[] GetAllNamespacesAndNodesets();

        string[] GetAllNamesAndNodesets();
    }
}