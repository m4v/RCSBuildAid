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

using UnityEngine;

namespace RCSBuildAid
{
    public class MenuTranslation : ModeContent
    {
        protected override PluginMode workingMode {
            get { return PluginMode.RCS; }
        }

        protected override void Setup()
        {
            if (Settings.show_rcs_twr && Settings.selected_body.atmosphere) {
                /* TWR is only correct for bodies without atmosphere */
                Settings.selected_body = Planetarium.fetch.Home.orbitingBodies[0];
            }
        }

        protected override void DrawContent ()
        {
            MarkerForces vesselForces = RCSBuildAid.VesselForces;
            GUILayout.BeginVertical ();
            {
                if (RCSBuildAid.HasRCS) {
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
                        GUILayout.Label (vesselForces.Torque ().magnitude.ToString ("0.### kNm"));
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal (); 
                    {
                        GUILayout.Label ("Thrust", MainWindow.style.readoutName);
                        GUILayout.Label (vesselForces.Thrust ().magnitude.ToString ("0.## kN"));
                    }
                    GUILayout.EndHorizontal ();
                    if (DeltaV.sanity) {
                        GUILayout.BeginHorizontal (); 
                        {
                            GUILayout.Label ("ΔV", MainWindow.style.readoutName);
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
                    if (Settings.show_rcs_twr) {
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("Body", MainWindow.style.readoutName);
                            if (GUILayout.Button(Settings.selected_body.name, MainWindow.style.clickLabel)) {
                                MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                                MainWindow.cBodyListMode = RCSBuildAid.Mode;
                            }
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label("TWR", MainWindow.style.readoutName);
                            GUILayout.Label(vesselForces.TWR.ToString("0.##"));
                        }
                        GUILayout.EndHorizontal();
                    }
                } else {
                    GUILayout.Label ("No RCS thrusters attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndVertical ();
        }
    }
}

