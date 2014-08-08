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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuDebug : ToggleableContent
    {
        string title = "DEBUG";
        bool showMenu = false;

        protected override string buttonTitle {
            get { return title; }
        }

        public override bool value {
            get { return showMenu; }
            set { showMenu = value; }
        }

        protected override void update ()
        {
        }

        protected override void content ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MomentOfInertia moi = comv.MoI;
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                GUILayout.BeginVertical (); 
                {
                    GUILayout.Label ("MoI:");
                    GUILayout.Label ("Ang Acc:");
                    GUILayout.Label ("Ang Acc:");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label (moi.value.ToString("0.## tm²"));
                    float angAcc = comv.Torque().magnitude / moi.value;
                    GUILayout.Label (angAcc.ToString ("0.## r/s²"));
                    GUILayout.Label ((angAcc * Mathf.Rad2Deg).ToString ("0.## °/s²"));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
            DebugSettings.labelMagnitudes = 
                GUILayout.Toggle(DebugSettings.labelMagnitudes, "Show vector magnitudes");
            DebugSettings.inFlightAngularInfo = 
                GUILayout.Toggle(DebugSettings.inFlightAngularInfo, "In flight angular data");
            DebugSettings.startInOrbit = 
                GUILayout.Toggle(DebugSettings.startInOrbit, "Launch in orbit");
        }
    }
}

