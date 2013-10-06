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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class Window : MonoBehaviour
    {
        enum WinState { none, RCS, Engine, Markers };

        int winID;
        Rect winRect;
        WinState state;
        bool softLock = false;
        bool minimized = false;
        string title = "RCS Build Aid v0.4";
        int winX = 300, winY = 200;
        int winWidth = 178, winHeight = 51;

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (winX, winY, winWidth, winHeight);
            Load ();
        }

        void Start ()
        {
            switchDisplayMode ();
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Load ()
        {
            state = (WinState)Settings.GetValue ("window_state", 0);
            winRect.x = Settings.GetValue ("window_x", winX);
            winRect.y = Settings.GetValue ("window_y", winY);

            /* check if within screen */
            winRect.x = Mathf.Clamp (winRect.x, 0, Screen.width - winWidth);
            winRect.y = Mathf.Clamp (winRect.y, 0, Screen.height - 233);
        }

        void Save ()
        {
            Settings.SetValue ("window_x", (int)winRect.x);
            Settings.SetValue ("window_y", (int)winRect.y);
            Settings.SetValue ("window_state", (int)state);
        }

        void OnGUI ()
        {
            if (RCSBuildAid.Enabled) {
                if (minimized) {
                    winRect = GUI.Window (winID, winRect, drawWindowMinimized, title);
                } else {
                    winRect = GUILayout.Window (winID, winRect, drawWindow, title);
                }
            }
            setEditorLock ();
            debug ();
        }

        void drawWindowMinimized (int ID)
        {
            minimizeButton();
            GUI.DragWindow ();
        }

        void drawWindow (int ID)
        {
            if (minimizeButton () && minimized) {
                return;
            }
            /* Main button bar */
            GUILayout.BeginHorizontal();
            for (int i = 1; i < 4; i++) {
                bool toggleState = (int)state == i;
                if (GUILayout.Toggle(toggleState, ((WinState)i).ToString(), GUI.skin.button)) {
                    if (!toggleState) {
                        /* toggling on */
                        state = (WinState)i;
                        switchDisplayMode();
                    }
                } else {
                    if (toggleState) {
                        /* toggling off */
                        state = WinState.none;
                        switchDisplayMode();
                    }
                }
            }
            GUILayout.EndHorizontal();

            /* check display Mode changed and sync GUI state */
            checkDisplayMode();

            switch (state) {
            case WinState.RCS:
                drawRCSMenu();
                break;
            case WinState.Engine:
                drawEngineMenu();
                break;
            case WinState.Markers:
                drawDCoMMenu();
                break;
            case WinState.none:
                winRect.height = winHeight;
                break;
            }

            GUI.DragWindow();
        }

        void switchDisplayMode ()
        {
            switch(state) {
            case WinState.RCS:
                RCSBuildAid.SetMode(DisplayMode.RCS);
                break;
            case WinState.Engine:
                RCSBuildAid.SetMode(DisplayMode.Engine);
                break;
            case WinState.none:
                RCSBuildAid.SetMode(DisplayMode.none);
                break;
            }
        }

        void checkDisplayMode ()
        {
            switch (state) {
            case WinState.Markers:
                break;
            default:
                switch(RCSBuildAid.mode) {
                case DisplayMode.none:
                    state = WinState.none;
                    break;
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

        bool minimizeButton ()
        {
            if (GUI.Button (new Rect (winRect.width - 15, 3, 12, 12), "")) {
                minimized = !minimized;
                if (minimized) {
                    GUI.skin.window.clipping = TextClipping.Overflow;
                    winRect = new Rect (winRect.x, winRect.y, winWidth, 26);
                } else {
                    GUI.skin.window.clipping = TextClipping.Clip;
                }
                return true;
            }
            return false;
        }

        void drawRCSMenu ()
        {
            winRect.height = 151; /* 5 rows, 26 + 25*5 */
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
            winRect.height = 126; /* 4 rows, 26 + 25*4 */
            drawTorqueLabel();
            drawRefButton();
        }

        void drawDCoMMenu ()
        {
            winRect.height = 233;
            bool com = RCSBuildAid.showCoM;
            bool dcom = RCSBuildAid.showDCoM;

            /* DCoM options */
            GUILayout.BeginVertical (GUI.skin.box);
            GUILayout.BeginHorizontal ();
            GUILayout.Label ("DCoM");
            dcom = GUILayout.Toggle (dcom, "Show");
            GUILayout.EndHorizontal ();
            if (dcom) {
                bool mono = DryCoM_Marker.monoprop;
                bool fuel = DryCoM_Marker.fuel;
                bool solid = DryCoM_Marker.solid;
                bool other = DryCoM_Marker.other;
                Vector3 offset = RCSBuildAid.CoM.transform.position
                                 - RCSBuildAid.DCoM.transform.position;

                GUILayout.Label (String.Format ("Dry mass: {0:F2} t", DryCoM_Marker.dryMass));
                GUILayout.Label (String.Format ("CoM offset: {0:F2} m", offset.magnitude));
                mono = GUILayout.Toggle (mono, "monopropellant");
                fuel = GUILayout.Toggle (fuel, "liquid fuel/oxidizer");
                solid = GUILayout.Toggle (solid, "solid fuel");
                other = GUILayout.Toggle (other, "other resources");

                DryCoM_Marker.monoprop = mono;
                DryCoM_Marker.fuel = fuel;
                DryCoM_Marker.oxidizer = fuel;
                DryCoM_Marker.solid = solid;
                DryCoM_Marker.other = other;
            } else {
                winRect.height = 117;
            }
            GUILayout.EndVertical();

            /* CoM options */
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("CoM");
            com = GUILayout.Toggle(com, "Show");
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            RCSBuildAid.showCoM = com;
            RCSBuildAid.showDCoM = dcom;
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

        bool isMouseOver ()
        {
            Vector2 position = new Vector2(Input.mousePosition.x,
                                           Screen.height - Input.mousePosition.y);
            return winRect.Contains(position);
        }

        /* Whenever we mouseover our window, we need to lock the editor so we don't pick up
         * parts while dragging the window around */
        void setEditorLock ()
        {
            if (RCSBuildAid.Enabled) {
                bool mouseOver = isMouseOver ();
                if (mouseOver && !EditorLogic.softLock && !softLock) {
                    softLock = true;
                    EditorLogic.SetSoftLock (true);
                } else if (!mouseOver && EditorLogic.softLock && softLock) {
                    softLock = false;
                    EditorLogic.SetSoftLock (false);
                }
            } else if (softLock && EditorLogic.softLock) {
                softLock = false;
                EditorLogic.SetSoftLock (false);
            }
        }

        /*
         * Debug stuff
         */

        Rect _oldRect;

        [Conditional("DEBUG")]
        void debug ()
        {
            if (_oldRect != winRect) {
                print (winRect.ToString ());
                _oldRect = winRect;
            }
        }

    }
}
