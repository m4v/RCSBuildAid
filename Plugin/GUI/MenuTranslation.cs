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

using UnityEngine;

namespace RCSBuildAid
{
    public class MenuTranslation : ModeContent
    {
        protected override PluginMode workingMode {
            get { return PluginMode.RCS; }
        }

        protected override void DrawContent ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            GUILayout.BeginVertical ();
            {
                if (RCSBuildAid.RCS.Count != 0) {
                    GUILayout.BeginHorizontal (); 
                    {
                        GUILayout.Label ("Reference", MainWindow.style.readoutName);
                        MainWindow.ReferenceButton ();
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal (); 
                    {
                        GUILayout.Label ("Direction", MainWindow.style.readoutName);
                        MainWindow.TranslationButton ();
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
                    if (DeltaV.sanity) {
                        GUILayout.BeginHorizontal (); 
                        {
                            GUILayout.Label ("Delta V", MainWindow.style.readoutName);
                            GUILayout.Label (DeltaV.dV.ToString ("0.# m/s"));
                        }
                        GUILayout.EndHorizontal ();
                        GUILayout.BeginHorizontal (); 
                        {
                            GUILayout.Label ("Burn time", MainWindow.style.readoutName);
                            GUILayout.Label (MainWindow.TimeFormat (DeltaV.burnTime));
                        }
                        GUILayout.EndHorizontal ();
                    }
                } else {
                    GUILayout.Label ("No RCS thrusters attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndVertical ();
        }
    }
}

