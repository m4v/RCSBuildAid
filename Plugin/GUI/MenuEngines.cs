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
    public static class MenuEngines
    {
        public static void onModeChange (PluginMode mode)
        {
            MainWindow.onDrawModeContent -= DrawContent;
            if (mode == PluginMode.Engine) {
                MainWindow.onDrawModeContent += DrawContent;
            }
        }

        public static void DrawContent ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MassEditorMarker comm = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ();
            double gravity = MainWindow.body.gMagnitudeAtCenter / Mathf.Pow ((float)MainWindow.body.Radius, 2);
            GUILayout.BeginHorizontal ();
            {
                if (RCSBuildAid.EngineList.Count != 0) {
                    GUILayout.BeginVertical ();
                    {
                        GUILayout.Label ("Reference");
                        GUILayout.Label ("Torque");
                        GUILayout.Label ("Thrust");
                        GUILayout.Label ("TWR");
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        MainWindow.referenceButton ();
                        GUILayout.Label (comv.Torque().magnitude.ToString ("0.## kNm"));
                        GUILayout.Label (comv.Thrust().magnitude.ToString ("0.## kN"));
                        GUILayout.Label ((comv.Thrust().magnitude / (comm.mass * gravity)).ToString("0.##"));
                    }
                    GUILayout.EndVertical ();
                } else {
                    GUILayout.Label("No engines attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label ("Body");
                    GUILayout.Label ("Gravity");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    if (GUILayout.Button (MainWindow.body.name, MainWindow.style.clickLabel)) {
                        MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                        MainWindow.cBodyListMode = RCSBuildAid.mode;
                    }
                    GUILayout.Label (String.Format ("{0:F2} m/s²", gravity));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
        }
    }
}

