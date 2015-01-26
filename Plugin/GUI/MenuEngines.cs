/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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

using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuEngines : ModeContent
    {
        protected override PluginMode workingMode {
            get { return PluginMode.Engine; }
        }

        protected override void DrawContent ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MassEditorMarker comm = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ();
            double gravity = MainWindow.engBody.gMagnitudeAtCenter / Mathf.Pow ((float)MainWindow.engBody.Radius, 2);
            GUILayout.BeginVertical ();
            {
                if (RCSBuildAid.EngineList.Count != 0) {
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Reference", MainWindow.style.readoutName);
                        MainWindow.referenceButton ();
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Rotation", MainWindow.style.readoutName);
                        MainWindow.rotationButtonWithReset ();
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Torque", MainWindow.style.readoutName);
                        GUILayout.Label (comv.Torque ().magnitude.ToString ("0.### kNm"));
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Thrust", MainWindow.style.readoutName);
                        GUILayout.Label (comv.Thrust ().magnitude.ToString ("0.## kN"));
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Body", MainWindow.style.readoutName);
                        if (GUILayout.Button (MainWindow.engBody.name, MainWindow.style.clickLabel)) {
                            MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                            MainWindow.cBodyListMode = RCSBuildAid.mode;
                        }
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("TWR", MainWindow.style.readoutName);
                        GUILayout.Label ((comv.Thrust ().magnitude / (comm.mass * gravity)).ToString ("0.##"));
                    }
                    GUILayout.EndHorizontal ();
                } else {
                    GUILayout.Label ("No engines attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndVertical ();
        }
    }
}

