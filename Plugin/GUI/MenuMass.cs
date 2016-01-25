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
    public class MenuMass : ToggleableContent
    {
        const string title = "Vessel mass";
        float mass;

        protected override string buttonTitle {
            get { return title; }
        }

        public override bool value {
            get { return Settings.menu_vessel_mass; }
            set { Settings.menu_vessel_mass = value; }
        }

        protected override void update ()
        {
            if (Settings.use_dry_mass) {
                mass = DCoMMarker.Mass;
            } else {
                mass = CoMMarker.Mass - DCoMMarker.Mass;
            }
        }

        protected override void content ()
        {
            /* Vessel stats */
            GUILayout.BeginVertical ();
            {
                GUILayout.BeginHorizontal ();
                {
                    GUILayout.Label ("Wet mass", MainWindow.style.readoutName);
                    GUILayout.Label (CoMMarker.Mass.ToString("0.### t"));
                }
                GUILayout.EndHorizontal ();
                GUILayout.BeginHorizontal ();
                {
                    if (GUILayout.Button (Settings.use_dry_mass ? "Dry mass" : "Fuel mass",
                            MainWindow.style.clickLabel, 
                            GUILayout.Width(Style.readout_label_width))) {
                        Settings.use_dry_mass = !Settings.use_dry_mass;
                    }
                    GUILayout.Label (mass.ToString("0.### t"));
                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical ();
        }
    }
}

