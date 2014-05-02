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
    public class MenuMass : ToggleableContent
    {
        string title = "Vessel mass";
        Vector3 offset = Vector3.zero;
        float mass = 0;

        protected override string buttonTitle {
            get { return title; }
        }

        public override bool value {
            get { return Settings.menu_vessel_mass; }
            set { Settings.menu_vessel_mass = value; }
        }

        protected override void update ()
        {
            offset = RCSBuildAid.CoM.transform.position - RCSBuildAid.DCoM.transform.position;
            if (Settings.use_dry_mass) {
                mass = DCoM_Marker.Mass;
            } else {
                mass = CoM_Marker.Mass - DCoM_Marker.Mass;
            }
        }

        protected override void content ()
        {
            /* Vessel stats */
            GUILayout.BeginHorizontal ();
            {
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label ("Wet mass");
                    if (GUILayout.Button (Settings.use_dry_mass ? "Dry mass" : "Fuel mass",
                                          MainWindow.style.clickLabel)) {
                        Settings.use_dry_mass = !Settings.use_dry_mass;
                    }
                    GUILayout.Label ("DCoM offset");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label (CoM_Marker.Mass.ToString("0.### t"));
                    GUILayout.Label (mass.ToString("0.### t"));
                    GUILayout.Label (offset.magnitude.ToString("0.## m"));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
        }
    }
}

