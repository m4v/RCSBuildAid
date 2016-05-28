/* Copyright © 2013-2016, Elián Hanisch <lambdae2@gmail.com>
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
            Settings.ModCompatibilityCheck ();
            new AppLauncher ();
        }
    }

    public static class Settings
    {
        const string configPath = "GameData/RCSBuildAid/Plugins/PluginData/settings.cfg";
        static string configAbsolutePath;
        static ConfigNode settings;

        public static bool toolbar_plugin_loaded;
        public static bool toolbar_plugin;
        public static Action toolbarSetup;

        public static MarkerType com_reference;
        public static PluginMode plugin_mode;
        public static Direction direction;
        public static bool menu_vessel_mass;
        public static bool menu_res_mass;
        public static float marker_scale;
        public static bool include_wheels;
        public static bool include_rcs;
        public static bool eng_include_rcs;
        public static bool engines_vac;
        public static bool resource_amount;
        public static bool use_dry_mass;
        public static bool show_marker_com;
        public static bool show_marker_dcom;
        public static bool show_marker_acom;
        public static bool marker_autoscale;
        public static bool menu_minimized;
        public static bool applauncher;
        public static bool action_screen;
        public static bool disable_mod_compatibility_check;
        public static int window_x;
        public static int window_y;

        // TODO refactor
        public static Dictionary<string, bool> resource_cfg = new Dictionary<string, bool> ();
        public static Dictionary<string, float> altitude_cfg = new Dictionary<string, float> ();
        public static CelestialBody selected_body;

        public static List<PluginMode> EnabledModes = new List<PluginMode> {
            PluginMode.RCS, 
            PluginMode.Attitude, 
            PluginMode.Engine, 
            PluginMode.Parachutes
        };

        public static void LoadConfig ()
        {
            configAbsolutePath = Path.Combine (KSPUtil.ApplicationRootPath, configPath);
            settings = ConfigNode.Load (configAbsolutePath) ?? new ConfigNode ();

            com_reference = (MarkerType)GetValue ("com_reference", (int)MarkerType.CoM);
            plugin_mode = (PluginMode)GetValue ("plugin_mode", (int)PluginMode.RCS);
            direction = (Direction)GetValue ("direction", (int)Direction.right);

            menu_vessel_mass = GetValue ("menu_vessel_mass", false);
            menu_res_mass    = GetValue ("menu_res_mass"   , false);
            marker_scale     = GetValue ("marker_scale"    , 1f   );
            include_rcs      = GetValue ("include_rcs"     , true );
            include_wheels   = GetValue ("include_wheels"  , false);
            eng_include_rcs  = GetValue ("eng_include_rcs" , false);
            resource_amount  = GetValue ("resource_amount" , false);
            use_dry_mass     = GetValue ("use_dry_mass"    , true );
            show_marker_com  = GetValue ("show_marker_com" , true );
            show_marker_dcom = GetValue ("show_marker_dcom", true );
            show_marker_acom = GetValue ("show_marker_acom", false);
            marker_autoscale = GetValue ("marker_autoscale", true );
            menu_minimized   = GetValue ("menu_minimized"  , false);
            applauncher      = GetValue ("applauncher"     , true );
            action_screen    = GetValue ("action_screen"   , false);
            toolbar_plugin   = GetValue ("toolbar_plugin"  , true );
            window_x         = GetValue ("window_x"        , 280  );
            window_y         = GetValue ("window_y"        , 114  );
            disable_mod_compatibility_check = GetValue ("disable_mod_compatibility_check", false);

            /* for these resources, set some defaults */
            resource_cfg ["LiquidFuel"] = GetValue (resourceKey ("LiquidFuel"), false);
            resource_cfg ["Oxidizer"]   = GetValue (resourceKey ("Oxidizer")  , false);
            resource_cfg ["SolidFuel"]  = GetValue (resourceKey ("SolidFuel") , false);
            resource_cfg ["XenonGas"]   = GetValue (resourceKey ("XenonGas")  , true );
            resource_cfg ["IntakeAir"]  = GetValue (resourceKey ("IntakeAir") , true );
            resource_cfg ["Ablator"]    = GetValue (resourceKey ("Ablator")   , true );
            resource_cfg ["Ore"]        = GetValue (resourceKey ("Ore")       , false );
            resource_cfg ["MonoPropellant"] = GetValue (resourceKey ("MonoPropellant"), true);

            string bodyname = GetValue ("selected_body", "Kerbin");
            selected_body = PSystemManager.Instance.localBodies.Find (b => b.name == bodyname);
            if (selected_body == null) {
                /* can happen in a corrupted settings.cfg */
                selected_body = Planetarium.fetch.Home;
            }

            Events.ConfigSaving += SaveConfig;
        }

        public static void SaveConfig ()
        {
            SetValue ("com_reference"   , (int)com_reference);
            SetValue ("plugin_mode"     , (int)plugin_mode);
            SetValue ("shortcut_key"    , PluginKeys.PLUGIN_TOGGLE.primary.ToString());
            SetValue ("menu_vessel_mass", menu_vessel_mass);
            SetValue ("menu_res_mass"   , menu_res_mass   );
            SetValue ("marker_scale"    , marker_scale    );
            SetValue ("include_rcs"     , include_rcs     );
            SetValue ("include_wheels"  , include_wheels  );
            SetValue ("eng_include_rcs" , eng_include_rcs );
            SetValue ("resource_amount" , resource_amount );
            SetValue ("use_dry_mass"    , use_dry_mass    );
            SetValue ("show_marker_com" , show_marker_com );
            SetValue ("show_marker_dcom", show_marker_dcom);
            SetValue ("show_marker_acom", show_marker_acom);
            SetValue ("marker_autoscale", marker_autoscale);
            SetValue ("selected_body"   , selected_body.name);
            SetValue ("menu_minimized"  , menu_minimized  );
            SetValue ("applauncher"     , applauncher     );
            SetValue ("action_screen"   , action_screen   );
            SetValue ("toolbar_plugin"  , toolbar_plugin  );
            SetValue ("window_x"        , window_x        );
            SetValue ("window_y"        , window_y        );
            SetValue ("disable_mod_compatibility_check", disable_mod_compatibility_check);

            if (direction != Direction.none) {
                SetValue ("direction", (int)direction);
            }
            foreach (string name in resource_cfg.Keys) {
                SetValue (resourceKey(name), resource_cfg [name]);
            }
            foreach (string name in altitude_cfg.Keys) {
                SetValue (altitudeKey(name), altitude_cfg [name]);
            }

            try {
                settings.Save (configAbsolutePath);
            } catch (System.IO.IsolatedStorage.IsolatedStorageException) {
                Debug.LogWarning (
                    string.Format(
                        "RCS Build Aid failed to save its config, check the path '{0}' exists",
                        Path.GetDirectoryName(configPath))
                );
            }
        }

        public static void ModCompatibilityCheck () {
            if (disable_mod_compatibility_check) {
                Debug.LogWarning ("RCSBuildAid's mod compatibility check disabled.");
                return;
            }
            foreach (var mod in AssemblyLoader.loadedAssemblies) {
                var assemblyName = mod.assembly.GetName();
                var modName = assemblyName.Name;
                if (String.Equals (modName, "FerramAerospaceResearch", StringComparison.OrdinalIgnoreCase) ||
                    String.Equals (modName, "RealChute", StringComparison.OrdinalIgnoreCase)) {
                    if (EnabledModes.Remove (PluginMode.Parachutes)) {
                        Debug.LogWarning ("RCSBuildAid's parachute mode disabled since incompatible mods were detected.");
                        if (Settings.plugin_mode == PluginMode.Parachutes) {
                            Settings.plugin_mode = PluginMode.none;
                        }
                    }
                }
            }
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
            return int.TryParse (settings.GetValue (key), out value) ? value : defaultValue;
        }

        public static bool GetValue (string key, bool defaultValue)
        {
            bool value;
            return bool.TryParse (settings.GetValue (key), out value) ? value : defaultValue;
        }

        public static float GetValue (string key, float defaultValue)
        {
            float value;
            return float.TryParse (settings.GetValue (key), out value) ? value : defaultValue;
        }

        public static string GetValue (string key, string defaultValue)
        {
            string value = settings.GetValue(key);
            return String.IsNullOrEmpty (value) ? defaultValue : value;
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

        public static float GetAltitudeCfg (string bodyName, float defaultValue)
        {
            float value;
            if (altitude_cfg.TryGetValue (bodyName, out value)) {
                return value;
            }
            string key = altitudeKey(bodyName);
            value = GetValue(key, defaultValue);
            altitude_cfg[bodyName] = value;
            return value;
        }

        static string altitudeKey(string name)
        {
            return "drag_altitude_" + name;
        }

        public static void setupToolbar(bool value) {
            Settings.toolbar_plugin = value;
            if (toolbarSetup != null) {
                toolbarSetup ();
            }
        }
    }

    public static class PluginKeys 
    {
        public static KeyBinding PLUGIN_TOGGLE;

        public static KeyBinding TRANSLATE_UP;
        public static KeyBinding TRANSLATE_DOWN;
        public static KeyBinding TRANSLATE_BACK;
        public static KeyBinding TRANSLATE_FWD;
        public static KeyBinding TRANSLATE_RIGHT;
        public static KeyBinding TRANSLATE_LEFT;

        public static void Setup ()
        {
            TRANSLATE_UP    = new KeyBinding (GameSettings.TRANSLATE_UP.primary);
            TRANSLATE_DOWN  = new KeyBinding (GameSettings.TRANSLATE_DOWN.primary);
            TRANSLATE_BACK  = new KeyBinding (GameSettings.TRANSLATE_BACK.primary);
            TRANSLATE_FWD   = new KeyBinding (GameSettings.TRANSLATE_FWD.primary);
            TRANSLATE_RIGHT = new KeyBinding (GameSettings.TRANSLATE_RIGHT.primary);
            TRANSLATE_LEFT  = new KeyBinding (GameSettings.TRANSLATE_LEFT.primary);

            PLUGIN_TOGGLE = new KeyBinding (Parse (Settings.GetValue ("shortcut_key", KeyCode.Alpha5.ToString ())));
        }

        public static KeyCode Parse(string value) {
            return (KeyCode)Enum.Parse (typeof(KeyCode), value);
        }
    }
}

