using System;
using System.Collections.Generic;

namespace Opc.Ua.Cloud.Library.Controllers
{
    public class UriMapper
    {

        // Helpers to make for more readable paths, etc.
        private static readonly Dictionary<string, string> TypeToPath = new Dictionary<string, string>
        {
            {"Asset Admin Shells","idta/asset" },
            {"Submodels","idta/submodels" },
            {"Another","Thing" },
        };


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

        public static string GetFriendlyName(string strName, Dictionary<string, string> d)
        {
            if (d.TryGetValue(strName, out string strShortName))
            {
                strName = strName.Replace(strName, strShortName, StringComparison.CurrentCulture);
            }
            return strName;
        }

    } // class
} // namespace
