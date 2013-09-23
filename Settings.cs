using System;
using System.IO;
using UnityEngine;

namespace RCSBuildAid
{
    public static class Settings
    {
        static string configFile = "GameData/RCSBuildAid/settings.cfg";
        static string configPath;
        static ConfigNode settings;

        public static void LoadConfig ()
        {
            configPath = Path.Combine (KSPUtil.ApplicationRootPath, configFile);
            settings = ConfigNode.Load (configPath) ?? new ConfigNode ();
        }

        public static void SaveConfig ()
        {
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
    }
}

