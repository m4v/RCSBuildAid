/* Copyright © 2013-2014, Elián Hanisch <lambdae2@gmail.com>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace RCSBuildAid
{
    public static class Settings
    {
        static string configFile = "GameData/RCSBuildAid/settings.cfg";
        static string configPath;
        static ConfigNode settings;

        public static PluginMode plugin_mode;
        public static bool menu_vessel_mass;
        public static bool menu_res_mass;
        public static float marker_scale;
        public static bool include_wheels;
        public static bool include_rcs;
        public static bool resource_amount;
        public static bool use_dry_mass;
        public static bool show_marker_com;
        public static bool show_marker_dcom;
        public static bool show_marker_acom;
        public static bool menu_minimized;
        public static bool toolbar_plugin;
        public static Dictionary<string, bool> resource_cfg = new Dictionary<string, bool> ();

        public static void LoadConfig ()
        {
            configPath = Path.Combine (KSPUtil.ApplicationRootPath, configFile);
            settings = ConfigNode.Load (configPath) ?? new ConfigNode ();

            plugin_mode      = (PluginMode)GetValue ("plugin_mode", (int)PluginMode.RCS);
            menu_vessel_mass = GetValue ("menu_vessel_mass", false);
            menu_res_mass    = GetValue ("menu_res_mass"   , false);
            marker_scale     = GetValue ("marker_scale"    , 1f   );
            include_rcs      = GetValue ("include_rcs"     , true );
            include_wheels   = GetValue ("include_wheels"  , false);
            resource_amount  = GetValue ("resource_amount" , false);
            use_dry_mass     = GetValue ("use_dry_mass"    , true );
            show_marker_com  = GetValue ("show_marker_com" , true );
            show_marker_dcom = GetValue ("show_marker_dcom", true );
            show_marker_acom = GetValue ("show_marker_acom", false);
            menu_minimized   = GetValue ("menu_minimized"  , false);
            toolbar_plugin   = GetValue ("toolbar_plugin"  , true );

            /* for these resources, set some defaults */
            resource_cfg ["LiquidFuel"] = GetValue (resourceKey ("LiquidFuel"), false);
            resource_cfg ["Oxidizer"]   = GetValue (resourceKey ("Oxidizer")  , false);
            resource_cfg ["SolidFuel"]  = GetValue (resourceKey ("SolidFuel") , false);
            resource_cfg ["XenonGas"]   = GetValue (resourceKey ("XenonGas")  , true );
            resource_cfg ["IntakeAir"]  = GetValue (resourceKey ("IntakeAir") , true );
            resource_cfg ["MonoPropellant"] = GetValue (resourceKey ("MonoPropellant"), true);
        }

        public static void SaveConfig ()
        {
            SetValue ("plugin_mode"     , (int)plugin_mode);
            SetValue ("menu_vessel_mass", menu_vessel_mass);
            SetValue ("menu_res_mass"   , menu_res_mass   );
            SetValue ("marker_scale"    , marker_scale    );
            SetValue ("include_rcs"     , include_rcs     );
            SetValue ("include_wheels"  , include_wheels  );
            SetValue ("resource_amount" , resource_amount );
            SetValue ("use_dry_mass"    , use_dry_mass    );
            SetValue ("show_marker_com" , show_marker_com );
            SetValue ("show_marker_dcom", show_marker_dcom);
            SetValue ("show_marker_acom", show_marker_acom);
            SetValue ("menu_minimized"  , menu_minimized  );
            SetValue ("toolbar_plugin"  , toolbar_plugin  );

            foreach (string name in resource_cfg.Keys) {
                SetValue (resourceKey(name), resource_cfg [name]);
            }
            settings.Save (configPath);
        }

        public static void SetValue (string key, object value)
        {
            if (settings.HasValue(key)) {
                settings.RemoveValue(key);
            }
            settings.AddValue(key, value);
        }

        public static int GetValue (string key, int defaultValue)
        {
            int value;
            if (int.TryParse(settings.GetValue(key), out value)) {
                return value;
            }
            return defaultValue;
        }

        public static bool GetValue (string key, bool defaultValue)
        {
            bool value;
            if (bool.TryParse(settings.GetValue(key), out value)) {
                return value;
            }
            return defaultValue;
        }

        public static float GetValue (string key, float defaultValue)
        {
            float value;
            if (float.TryParse (settings.GetValue (key), out value)) {
                return value;
            }
            return defaultValue;
        }

        public static string GetValue (string key, string defaultValue)
        {
            string value = settings.GetValue(key);
            if (!String.IsNullOrEmpty(value)) {
                return value;
            }
            return defaultValue;
        }

        public static bool GetResourceCfg (string resName, bool defaultValue)
        {
            bool value;
            if (resource_cfg.TryGetValue (resName, out value)) {
                return value;
            }
            string key = resourceKey(resName);
            value = GetValue(key, defaultValue);
            resource_cfg[resName] = value;
            return value;
        }

        static string resourceKey(string name)
        {
            return "drycom_" + name;
        }
    }
}

