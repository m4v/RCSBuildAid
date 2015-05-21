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
        GimbalsControl gimbals;

        void Awake ()
        {
            gimbals = gameObject.AddComponent<GimbalsControl> ();
            gimbals.value = false;
        }

        protected override PluginMode workingMode {
            get { return PluginMode.Engine; }
        }

        protected override void DrawContent ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            GUILayout.BeginVertical ();
            {
                if (RCSBuildAid.Engines.Count != 0) {
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Reference", MainWindow.style.readoutName);
                        MainWindow.ReferenceButton ();
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
                        GUILayout.Label ("Thrust", MainWindow.style.readoutName, GUILayout.Width(40));
                        if (GUILayout.Button (Settings.engines_vac ? "Vac" : "ASL", MainWindow.style.clickLabel, GUILayout.Width(36))) {
                            Settings.engines_vac = !Settings.engines_vac;
                        }
                        GUILayout.Label (comv.Thrust ().magnitude.ToString ("0.## kN"));
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("Body", MainWindow.style.readoutName);
                        if (GUILayout.Button (Settings.selected_body.name, MainWindow.style.clickLabel)) {
                            MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                            MainWindow.cBodyListMode = RCSBuildAid.Mode;
                        }
                    }
                    GUILayout.EndHorizontal ();
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.Label ("TWR", MainWindow.style.readoutName);
                        GUILayout.Label (comv.TWR.ToString ("0.##"));
                    }
                    GUILayout.EndHorizontal ();
                    gimbals.DrawContent ();
                } else {
                    GUILayout.Label ("No engines attached", MainWindow.style.centerText);
                }
            }
            GUILayout.EndVertical ();
        }
    }

    public class GimbalsControl : ToggleableContent
    {
        void Awake ()
        {
            RCSBuildAid.events.DirectionChanged += onDirectionChange;
        }

        void onDirectionChange(Direction d) {
            if (RCSBuildAid.Mode == PluginMode.Engine && d != Direction.none) {
                value = true;
            }
        }

        #region implemented abstract members of ToggleableContent
        protected override void update ()
        {
        }

        protected override void content ()
        {
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Rotation", MainWindow.style.readoutName);
                MainWindow.RotationButton ();
            }
            GUILayout.EndHorizontal ();
            Settings.eng_include_rcs = GUILayout.Toggle (Settings.eng_include_rcs, "Include RCS");
        }

        protected override string buttonTitle {
            get { return "Gimbals"; }
        }
        #endregion

        protected override void onToggle ()
        {
            if (!value) {
                RCSBuildAid.SetDirection(Direction.none);
            }
        }
    }
}

