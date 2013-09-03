/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
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
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class Window : MonoBehaviour
    {
        Rect winPos;
        int winID;
        int winWidth = 100, winHeight = 50;
        string title = "RCSBuildAid";

        enum WinState { RCS, Engine, DCoM, Count };

        WinState state;

        delegate void drawMenuDelegate ();
        Dictionary<WinState, drawMenuDelegate> Menus;

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winPos = new Rect (300, 200, winWidth, winHeight);
            Menus = new Dictionary<WinState, drawMenuDelegate>();
            Menus[WinState.RCS] = drawRCSMenu;
            Menus[WinState.Engine] = drawEngineMenu;
            Menus[WinState.DCoM] = drawDCoMMenu;

            state = WinState.RCS;
        }

        void OnGUI ()
        {
            if (RCSBuildAid.CoM == null) {
                return;
            }

            if (RCSBuildAid.CoM.activeInHierarchy) {
                winPos = GUILayout.Window (winID, winPos, drawWindow, title);
            }
        }

        void drawWindow (int ID)
        {
            /* Main button bar */
            GUILayout.BeginHorizontal();
            for (int i = 0; i < (int)WinState.Count; i++) {
                if (GUILayout.Toggle((int)state == i, ((WinState)i).ToString(), GUI.skin.button)) {
                    state = (WinState)i;
                }
            }
            GUILayout.EndHorizontal();

            Menus[state]();
            GUI.DragWindow();
        }

        void drawRCSMenu ()
        {
            GUILayout.Label ("Turn rate: 100.00");
            drawRefButton();
            if (GUILayout.Button ("Mode: " + RCSBuildAid.rcsMode)) {
                int m = (int)RCSBuildAid.rcsMode + 1;
                if (m == 2) {
                    m = 0;
                }
                RCSBuildAid.rcsMode = (RCSMode)m;
            }
        }

        void drawEngineMenu ()
        {
            GUILayout.Label ("Turn rate: 100.00");
            drawRefButton();
        }

        void drawDCoMMenu ()
        {
            bool mono = DryCoM_Marker.monopropellant;
            bool fuel = DryCoM_Marker.fuel;
            bool other = DryCoM_Marker.other;

            mono = GUILayout.Toggle(mono, resourceToggleName("monopropellant", mono));
            fuel = GUILayout.Toggle(fuel, resourceToggleName("fuel + oxidizer", fuel));
            other = GUILayout.Toggle(other, resourceToggleName("other resources", other));

            DryCoM_Marker.monopropellant = mono;
            DryCoM_Marker.fuel = fuel;
            DryCoM_Marker.oxidizer = fuel;
            DryCoM_Marker.other = other;
        }

        string resourceToggleName (string name, bool enabled)
        {
            if (enabled) {
                return String.Format ("With {0}", name);
            } else {
                return String.Format ("Without {0}", name);
            }
        }

        void drawRefButton ()
        {
            if (GUILayout.Button ("Reference: " + RCSBuildAid.reference)) {
                int i = (int)RCSBuildAid.reference + 1;
                if (i == 2) {
                    i = 0;
                }
                RCSBuildAid.SetReference((CoMReference)i);
            }
        }
    }
}

