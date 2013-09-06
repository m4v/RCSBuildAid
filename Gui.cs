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
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class Window : MonoBehaviour
    {
        enum WinState { none, RCS, Engine, DCoM };

        delegate void drawMenuDelegate ();

        int winID;
        Rect winRect;
        WinState state;
        ConfigNode settings;
        string settingsFile;
        string title = "RCSBuildAid";
        int winX = 300, winY = 200;
        int winWidth = 172, winHeight = 51;
        /* fixed width: 172 */
        /* height = 26 + 25 * rows */
        Dictionary<WinState, drawMenuDelegate> Menus;

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (winX, winY, winWidth, winHeight);
            Menus = new Dictionary<WinState, drawMenuDelegate>();
            Menus[WinState.RCS] = drawRCSMenu;
            Menus[WinState.Engine] = drawEngineMenu;
            Menus[WinState.DCoM] = drawDCoMMenu;
            Menus[WinState.none] = delegate () {};

            Load ();
        }

        void OnDestroy ()
        {
            Save ();
        }

        void Load ()
        {
            int i;
            bool b;
            settingsFile = Path.Combine (KSPUtil.ApplicationRootPath,
                                       "GameData/RCSBuildAid/settings.cfg");
            settings = ConfigNode.Load (settingsFile) ?? new ConfigNode ();

            if (int.TryParse (settings.GetValue ("window_x"), out i)) {
                winRect.x = i;
            }
            if (int.TryParse (settings.GetValue ("window_y"), out i)) {
                winRect.y = i;
            }
            if (int.TryParse (settings.GetValue ("window_state"), out i)) {
                state = (WinState)i;
                switchMode ();
            } else {
                state = WinState.none;
            }
            if (bool.TryParse (settings.GetValue ("drycom_other"), out b)) {
                DryCoM_Marker.other = b;
            }
            if (bool.TryParse (settings.GetValue ("drycom_fuel"), out b)) {
                DryCoM_Marker.fuel = b;
                DryCoM_Marker.oxidizer = b;
            }
            if (bool.TryParse (settings.GetValue ("drycom_mono"), out b)) {
                DryCoM_Marker.monopropellant = b;
            }
            if (int.TryParse (settings.GetValue ("com_reference"), out i)) {
                RCSBuildAid.SetReference ((CoMReference)i);
            }
            if (int.TryParse (settings.GetValue ("rcs_mode"), out i)) {
                RCSBuildAid.rcsMode = (RCSMode)i;
            }
        }

        void Save ()
        {
            settings.ClearValues ();
            settings.AddValue ("window_x", (int)winRect.x);
            settings.AddValue ("window_y", (int)winRect.y);
            settings.AddValue ("window_state", (int)state);
            settings.AddValue ("drycom_other", DryCoM_Marker.other);
            settings.AddValue ("drycom_fuel", DryCoM_Marker.fuel);
            settings.AddValue ("drycom_mono", DryCoM_Marker.monopropellant);
            settings.AddValue ("com_reference", (int)RCSBuildAid.reference);
            settings.AddValue ("rcs_mode", (int)RCSBuildAid.rcsMode);
            settings.Save (settingsFile);
        }

        void OnGUI ()
        {
            if (RCSBuildAid.CoM == null) {
                return;
            }

            if (RCSBuildAid.CoM.activeInHierarchy) {
                winRect = GUI.Window (winID, winRect, drawWindow, title);
            }
        }

        void drawWindow (int ID)
        {
            /* Main button bar */
            GUILayout.BeginHorizontal();
            for (int i = 1; i < 4; i++) {
                bool toggleState = (int)state == i;
                if (GUILayout.Toggle(toggleState, ((WinState)i).ToString(), GUI.skin.button)) {
                    if (!toggleState) {
                        /* toggling on */
                        state = (WinState)i;
                        switchMode();
                    }
                } else {
                    if (toggleState) {
                        /* toggling off */
                        state = WinState.none;
                        switchMode();
                    }
                }
            }
            GUILayout.EndHorizontal();

            /* check display Mode changed and sync GUI state */
            checkMode();

            Menus[state]();
            GUI.DragWindow();
        }

        void switchMode ()
        {
            switch(state) {
            case WinState.RCS:
                RCSBuildAid.SetMode(DisplayMode.RCS);
                winRect.height = 151; /* 5 rows, 26 + 25*5 */
                break;
            case WinState.Engine:
                RCSBuildAid.SetMode(DisplayMode.Engine);
                winRect.height = 126; /* 4 rows, 26 + 25*4 */
                break;
            case WinState.none:
                RCSBuildAid.SetMode(DisplayMode.none);
                winRect.height = 51;
                break;
            case WinState.DCoM:
                winRect.height = 151;
                break;
            }
        }

        void checkMode ()
        {
            switch (state) {
            case WinState.RCS:
            case WinState.Engine:
                if (RCSBuildAid.mode == DisplayMode.none) {
                    state = WinState.none;
                }
                break;
            case WinState.none:
                switch (RCSBuildAid.mode) {
                case DisplayMode.Engine:
                    state = WinState.Engine;
                    break;
                case DisplayMode.RCS:
                    state = WinState.RCS;
                    break;
                }
                break;
            }
        }

        void drawRCSMenu ()
        {
            drawTorqueLabel();
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
            drawTorqueLabel();
            drawRefButton();
        }

        void drawDCoMMenu ()
        {
            bool mono = DryCoM_Marker.monopropellant;
            bool fuel = DryCoM_Marker.fuel;
            bool other = DryCoM_Marker.other;

            GUILayout.Label(String.Format ("Dry mass: {0:F2} t", DryCoM_Marker.dryMass));
            mono = GUILayout.Toggle(mono, resourceToggleName("monopropellant", mono));
            fuel = GUILayout.Toggle(fuel, resourceToggleName("fuel/oxidizer", fuel));
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

        void drawTorqueLabel ()
        {
            CoMVectors comv = RCSBuildAid.Reference.GetComponent<CoMVectors> ();
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label ("Torque:");
            GUILayout.Label ("Translation:");
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label (String.Format ("{0:F2} kNm", comv.valueTorque));
            GUILayout.Label (String.Format ("{0:F2} kN", comv.valueTranslation));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }
    }
}
