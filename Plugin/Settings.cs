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
using UnityEngine;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class SettingsLoader : MonoBehaviour
    {
        SettingsLoader ()
        {
            Settings.LoadConfig ();
        }
    }

    public static class Settings
    {
        static string configPath = "GameData/RCSBuildAid/settings.cfg";
        static string configAbsolutePath;
        static ConfigNode settings;

        public static bool toolbar_plugin_loaded = false;
        public static bool toolbar_plugin;
        public static Action toolbarSetup;

        public static MarkerType com_reference;
        public static PluginMode plugin_mode;
        public static Directions direction;
        public static KeyCode shortcut_key;
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
        public static string engine_cbody;
        public static bool menu_minimized;
        public static bool applauncher;
        public static bool action_screen;
        public static int window_x;
        public static int window_y;
        public static Dictionary<string, bool> resource_cfg = new Dictionary<string, bool> ();

        public static void LoadConfig ()
        {
            configAbsolutePath = Path.Combine (KSPUtil.ApplicationRootPath, configPath);
            settings = ConfigNode.Load (configAbsolutePath) ?? new ConfigNode ();

            com_reference = (MarkerType)GetValue ("com_reference", (int)MarkerType.CoM);
            plugin_mode = (PluginMode)GetValue ("plugin_mode", (int)PluginMode.RCS);
            direction = (Directions)GetValue ("direction", (int)Directions.right);
            shortcut_key = (KeyCode)GetValue ("shortcut_key", (int)KeyCode.None);

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
            engine_cbody     = GetValue ("engine_cbody"    , "Kerbin");
            menu_minimized   = GetValue ("menu_minimized"  , false);
            applauncher      = GetValue ("applauncher"     , true );
            action_screen    = GetValue ("action_screen"   , false);
            toolbar_plugin   = GetValue ("toolbar_plugin"  , true );
            window_x         = GetValue ("window_x"        , 280  );
            window_y         = GetValue ("window_y"        , 114  );

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
            SetValue ("com_reference"   , (int)com_reference);
            SetValue ("plugin_mode"     , (int)plugin_mode);
            SetValue ("shortcut_key"    , (int)shortcut_key);
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
            SetValue ("engine_cbody"    , engine_cbody    );
            SetValue ("menu_minimized"  , menu_minimized  );
            SetValue ("applauncher"     , applauncher     );
            SetValue ("action_screen"   , action_screen   );
            SetValue ("toolbar_plugin"  , toolbar_plugin  );
            SetValue ("window_x"        , window_x        );
            SetValue ("window_y"        , window_y        );

            if (direction != Directions.none) {
                SetValue ("direction", (int)direction);
            }
            foreach (string name in resource_cfg.Keys) {
                SetValue (resourceKey(name), resource_cfg [name]);
            }
            settings.Save (configAbsolutePath);
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
            if (String.IsNullOrEmpty(value)) {
                return defaultValue;
            }
            return value;
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

        public static void setupToolbar(bool value) {
            Settings.toolbar_plugin = value;
            if (toolbarSetup != null) {
                toolbarSetup ();
            }
        }
    }
}

