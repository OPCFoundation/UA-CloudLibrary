using System;
using System.Collections.Generic;
using AdminShell;

namespace Opc.Ua.Cloud.Library
{
    public class NameMgr
    {
        private static string strBaseUrl = "http://example.com/";
        private static string strBaseUrlAssetAdminShell = "http://example.com/idta/aas/";
        private static string strBaseUrlSubmodels = "http://example.com/idta/sum/";
        private static string strBaseUrlConceptDescription = "http://example.com/idta/cod/";

        private static readonly Dictionary<string, string> ShortNames = new Dictionary<string, string>
        {
            {"urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_battery_battery_pass_6_0_0_BatteryPass/","BatteryPassport" },
            {"urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport","DigitalProductPassport" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_generic_digital_product_passport_5_0_0_DigitalProductPassport/","DigitalProductPassport" },
            {"urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_pcf_7_0_0_Pcf/","ProductCarbonFootprint" },
            {"urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
            {"http://catena-x.org/UA/urn_samm_io_catenax_single_level_bom_as_built_3_0_0_SingleLevelBomAsBuilt/","SingleLevelBomAsBuilt" },
        };

        public static string GetShortName(string strName)
        {
            strName = GetBaseNodeFromNode(strName);
            strName = strName.Replace("nsu=http://catena-x.org/UA/", "", StringComparison.CurrentCulture);
            ShortNames.TryGetValue(strName, out strName);
            return strName;
        }

        // Helpers to make for more readable paths, etc.
        private static readonly Dictionary<string, string> PathFromType = new Dictionary<string, string>
        {
            {"Asset Admin Shells","idta/aas" },
            {"Submodels","idta/sum" },
            {"Concept Description","idta/cod" },
        };

        public static string GetPathFromType(string strName)
        {
            PathFromType.TryGetValue(strName, out strName);
            return strName;
        }

        public static string MakeNiceUrl(string strType, NodesetViewerNode nodesetview)
        {
            string strNode = NameMgr.GetShortName(nodesetview.Value);
            string strPath = GetPathFromType(strType);
            string strUrl = String.Empty;
            if (strType == "Asset Admin Shells")
               strUrl = $"{strBaseUrl}{strPath}/{strNode}/{nodesetview.Id}";
            else if (strType == "Submodels")
                strUrl = $"{strBaseUrl}{strPath}/{strNode}/{nodesetview.Id}";


            return strUrl;
        }
        

        public static string MakeNiceUrl(string strType, string strNodeId, string databaseID, List<string> listSubItems)
        {
            string strNode = NameMgr.GetShortName(strNodeId);
            string strPath = GetPathFromType(strType);
            string strUrl = $"{strBaseUrl}{strPath}/{strNode}/{databaseID}";
            if (strType == "Asset Admin Shells")
            {
                strUrl = $"{strBaseUrl}{strPath}/{strNode}/{databaseID}";
            }
            else if (strType == "Submodels")
            {
                strUrl = $"{strBaseUrl}{strPath}/{strNode}/{databaseID}";
                if (listSubItems != null && listSubItems.Count > 0)
                {
                    foreach (var item in listSubItems)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            strUrl += "/" + item.Trim('/');
                        }
                    }
                }
            }

            return strUrl;
        }

        private static string GetIdFromNode(string strInput)
        {
            int iSemicolon = strInput.IndexOf(';', StringComparison.CurrentCulture);
            if (iSemicolon > 0)
                strInput = strInput.Substring(iSemicolon + 1);

            return strInput;
        }

        private static string GetBaseNodeFromNode(string strInput)
        {
            int iSemicolon = strInput.IndexOf(';', StringComparison.CurrentCulture);
            if (iSemicolon > 0)
                strInput = strInput.Substring(0, iSemicolon -1);

            return strInput;
        }

        private static char[] astrSlash = {'/'};

        public static string GetDatabaseIDFromUrl(string strUrl)
        {
            string strRet = String.Empty;
            if (!String.IsNullOrEmpty(strUrl))
            {
                if (strUrl.StartsWith(strBaseUrlAssetAdminShell, StringComparison.CurrentCulture))
                {
                    strRet = strUrl.Replace(strBaseUrlAssetAdminShell, "", StringComparison.CurrentCulture);
                }
                else if (strUrl.StartsWith(strBaseUrlSubmodels, StringComparison.CurrentCulture))
                {
                    strRet = strUrl.Replace(strBaseUrlSubmodels, "", StringComparison.CurrentCulture);
                }
                else if (strUrl.StartsWith(strBaseUrlConceptDescription, StringComparison.CurrentCulture))
                {
                    strRet = strUrl.Replace(strBaseUrlConceptDescription, "", StringComparison.CurrentCulture);
                }

                int iSlash = strRet.IndexOf('/', StringComparison.CurrentCulture);
                if (iSlash > 0)
                {
                    string [] astrParts = strRet.Split(astrSlash, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < astrParts.Length; i++)
                    {
                        if (Int64.TryParse(astrParts[i], out Int64 Value))
                        {
                            return Value.ToString();
                        }
                    }
                }
            }
            return strRet;
        }

    }
}
