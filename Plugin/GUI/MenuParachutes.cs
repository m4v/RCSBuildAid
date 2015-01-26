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
        public static float speed;
        public static float altitude;
        public static float ground_height;
        public static float altitude_over_ground;
        public static float terminalVelocity;


        protected override PluginMode workingMode {
            get { return PluginMode.Parachutes; }
        }

        float calculateTerminalV (float altitude)
        {
            float density = MainWindow.dragBody.density(altitude);
            float gravity = MainWindow.dragBody.gravity(altitude);
            return Mathf.Sqrt ((2 * gravity) / (density * CoDMarker.drag_coef * FlightGlobals.DragMultiplier));
        }

        void Update ()
        {
            terminalVelocity = calculateTerminalV (altitude);
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
                MainWindow.referenceButton ();
            }
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Terminal V", MainWindow.style.readoutName);
                GUILayout.Label (String.Format ("{0:0.#} m/s", terminalVelocity));
            }
            GUILayout.EndHorizontal ();
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Label ("Body", MainWindow.style.readoutName);
                if (GUILayout.Button (MainWindow.dragBody.theName, MainWindow.style.clickLabel)) {
                    MainWindow.cBodyListEnabled = !MainWindow.cBodyListEnabled;
                    MainWindow.cBodyListMode = RCSBuildAid.mode;
                }
            }
            GUILayout.EndHorizontal ();
        }
    }
}

