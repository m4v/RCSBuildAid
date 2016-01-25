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

        void updateAltitude ()
        {
            /* the slider is used for select ground height, we don't know the height of the planet's 
             * highest peak, but we have the atmosphere as an upper bound */
            double atmAlt = Settings.selected_body.atmosphereDepth;
            /* the slider shouldn't go all the way up to planet's maximum atmosphere altitude */
            float maxAltTerrain = Mathf.Round ((float)(atmAlt * 0.25 / 1000)) * 1000f;
            float altSlider = Settings.GetAltitudeCfg (Settings.selected_body.name, 0f);
            /* exp scale */
            terrain_height = 0.01010101f * maxAltTerrain * (Mathf.Pow (10, 2 * altSlider) - 1);
            altitude = distance_from_terrain + terrain_height;
        }

        void Update ()
        {
            distance_from_terrain = 0;
            updateAltitude ();
        }

        protected override void DrawContent ()
        {
            if (EditorLogic.RootPart == null) {
                GUILayout.Label ("No vessel parts", MainWindow.style.centerText);
                return;
            }
            GUILayout.BeginVertical ();
            {
                if (CoDMarker.hasParachutes) {
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Reference", MainWindow.style.readoutName);
                        MainWindow.ReferenceButton ();
                    }
                    GUILayout.EndHorizontal ();
//                    GUILayout.BeginHorizontal ();
//                    {
//                        GUILayout.Label ("Cd", MainWindow.style.readoutName);
//                        GUILayout.Label (String.Format ("{0:0.#}", CoDMarker.Cd));
//                    }
//                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Vt", MainWindow.style.readoutName);
                        GUILayout.Label (String.Format ("{0:0.#} m/s", CoDMarker.Vt));
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Body", MainWindow.style.readoutName);
                        if (GUILayout.Button (Settings.selected_body.theName, MainWindow.style.clickLabel)) {
                            MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                            MainWindow.cBodyListMode = RCSBuildAid.Mode;
                        }
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Touchdown", MainWindow.style.readoutName);
                        if (GUILayout.Button (String.Format ("{0:F0} m", terrain_height), MainWindow.style.clickLabel)) {
                            show_altitude_slider = !show_altitude_slider;
                        }
                    }
                    GUILayout.EndHorizontal ();
                    if (show_altitude_slider) {
                        Settings.altitude_cfg [Settings.selected_body.name] = GUILayout.HorizontalSlider (
                            Settings.altitude_cfg [Settings.selected_body.name], 0f, 1f);
                    }
                } else {
                    GUILayout.Label ("No parachutes attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndVertical ();
        }
    }
}

