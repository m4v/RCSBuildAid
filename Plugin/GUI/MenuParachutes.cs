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
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuParachutes : ModeContent
    {
        public static float altitude;
        public static float terminal_velocity;

        bool show_altitude_slider = false;
        float terrain_height;
        float distance_from_terrain;

        protected override PluginMode workingMode {
            get { return PluginMode.Parachutes; }
        }

        float calculateTerminalV (float altitude)
        {
            float density = MainWindow.chuteBody.density(altitude);
            float gravity = MainWindow.chuteBody.gravity(altitude);
            return Mathf.Sqrt ((2 * gravity) / (density * CoDMarker.drag_coef * FlightGlobals.DragMultiplier));
        }

        void updateAltitude ()
        {
            /* the slider is used for select ground height, we don't know the height of the planet's 
             * highest peak, but we have the atmosphere as an upper bound */
            float atmAlt = MainWindow.chuteBody.maxAtmosphereAltitude;
            /* the slider shouldn't go all the way up to planet's maximum atmosphere altitude */
            float maxAltTerrain = Mathf.Round (atmAlt * 0.25f / 1000f) * 1000f;
            float altSlider = Settings.GetAltitudeCfg (MainWindow.chuteBody.name, 0f);
            /* exp scale */
            terrain_height = 0.01010101f * maxAltTerrain * (Mathf.Pow (10, 2 * altSlider) - 1);
            altitude = distance_from_terrain + terrain_height;
        }

        void Update ()
        {
            distance_from_terrain = 0;
            updateAltitude ();
            terminal_velocity = calculateTerminalV (altitude);
        }

        protected override void DrawContent ()
        {
            if (EditorLogic.RootPart == null) {
                GUILayout.Label ("No vessel parts", MainWindow.style.centerText);
                return;
            }
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Reference", MainWindow.style.readoutName);
                MainWindow.ReferenceButton ();
            }
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Terminal V", MainWindow.style.readoutName);
                GUILayout.Label (String.Format ("{0:0.#} m/s", terminal_velocity));
            }
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Body", MainWindow.style.readoutName);
                if (GUILayout.Button (MainWindow.chuteBody.theName, MainWindow.style.clickLabel)) {
                    MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                    MainWindow.cBodyListMode = RCSBuildAid.Mode;
                }
            }
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Terrain height", MainWindow.style.readoutName);
                if (GUILayout.Button (String.Format ("{0:F0} m", terrain_height), MainWindow.style.clickLabel)) {
                    show_altitude_slider = !show_altitude_slider;
                }
            }
            GUILayout.EndHorizontal ();
            if (show_altitude_slider) {
                Settings.altitude_cfg [MainWindow.chuteBody.name] = GUILayout.HorizontalSlider (
                    Settings.altitude_cfg [MainWindow.chuteBody.name], 0f, 1f);
            }
        }
    }
}

