using UACloudLibrary.Models;

namespace UACloudLibrary
{
    public interface IDatabase
    {
        string FindNodesets(string[] keywords);

        bool AddMetaDataToNodeSet(uint nodesetId, string name, string value);

        bool AddUATypeToNodeset(uint nodesetId, UATypes uaType, string browseName, string displayName, string nameSpace);

        bool DeleteAllRecordsForNodeset(uint nodesetId);
    }
}