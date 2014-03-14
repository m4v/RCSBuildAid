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

namespace RCSBuildAid
{
    public static class Settings
    {
        static string configFile = "GameData/RCSBuildAid/settings.cfg";
        static string configPath;
        static ConfigNode settings;

        public static float marker_scale;

        public static void LoadConfig ()
        {
            configPath = Path.Combine (KSPUtil.ApplicationRootPath, configFile);
            settings = ConfigNode.Load (configPath) ?? new ConfigNode ();
            marker_scale = Settings.GetValue ("marker_scale", 1f);
        }

        public static void SaveConfig ()
        {
            SetValue ("marker_scale", marker_scale);
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
    }
}

