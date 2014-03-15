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
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class Window : MonoBehaviour
    {
        enum WinState { none, Attitude, RCS, Engine, Mass, Debug };
#if DEBUG
        WinState endState = WinState.Debug;
#else
        WinState endState = WinState.Mass;
#endif

        int winID;
        Rect winRect;
        WinState state;
        bool softLock = false;
        bool minimized = false;
        string title = "RCS Build Aid v0.4.5";
        string shortTitle = "RBA";
        int winX = 300, winY = 200;
        int minWidth = 81;
        int maxWidth = 241;
        int minHeight = 51;
        int maxHeight = 238;
        int minimizedWidth = 241;
        int minimizedHeight = 26;

        GUIStyle centerText;
        GUIStyle labelButton;
        GUIStyle barButton;

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (winX, winY, minWidth, minHeight);
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
            winRect.x = Mathf.Clamp (winRect.x, 0, Screen.width - maxWidth);
            winRect.y = Mathf.Clamp (winRect.y, 0, Screen.height - maxHeight);
        }

        void Save ()
        {
            Settings.SetValue ("window_x", (int)winRect.x);
            Settings.SetValue ("window_y", (int)winRect.y);
            Settings.SetValue ("window_state", (int)state);
        }

        void OnGUI ()
        {
            switch (HighLogic.LoadedScene) {
            case GameScenes.EDITOR:
            case GameScenes.SPH:
                break;
            default:
                return;
            }

            /* style */
            GUI.skin.label.padding = new RectOffset ();
            GUI.skin.label.wordWrap = false;
            GUI.skin.toggle.padding = new RectOffset (15, 0, 0, 0);
            GUI.skin.toggle.overflow = new RectOffset (0, 0, -1, 0);

            if (centerText == null) {
                centerText = new GUIStyle (GUI.skin.label);
                centerText.alignment = TextAnchor.MiddleCenter;
                centerText.wordWrap = true;
            }
            if (labelButton == null) {
                float labelHeight = centerText.CalcHeight (new GUIContent ("right"), 100);
                labelButton = new GUIStyle (GUI.skin.button);
                labelButton.clipping = TextClipping.Overflow;
                labelButton.fixedHeight = labelHeight;
            }
            if (barButton == null) {
                Vector2 size = GUI.skin.button.CalcSize(new GUIContent("Attitude"));
                barButton = new GUIStyle(GUI.skin.button);
                barButton.fixedWidth = size.x;
                barButton.normal = GUI.skin.label.normal;
            }

            if (RCSBuildAid.Enabled) {
                string _title = title;
                if (state == WinState.none) {
                    _title = shortTitle;
                }
                if (minimized) {
                    GUI.skin.window.clipping = TextClipping.Overflow;
                    winRect.height = minimizedHeight;
                    winRect.width = minimizedWidth;
                    winRect = GUI.Window (winID, winRect, drawWindowMinimized, _title);
                } else {
                    GUI.skin.window.clipping = TextClipping.Clip;
                    if (Event.current.type == EventType.Layout) {
                        winRect.height = minHeight;
                        if (state == WinState.none) {
                            winRect.width = minWidth;
                        } else {
                            winRect.width = maxWidth;
                        }
                    }
                    winRect = GUILayout.Window (winID, winRect, drawWindow, _title);
                }
            }
            if (Event.current.type == EventType.Repaint) {
                setEditorLock ();
            }

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
            GUILayout.BeginHorizontal ();
            {
                GUILayout.BeginVertical ();
                {
                    for (int i = 1; i < (int)endState + 1; i++) {
                        bool toggleState = (int)state == i;
                        if (GUILayout.Toggle (toggleState, ((WinState)i).ToString (), barButton)) {
                            if (!toggleState) {
                                /* toggling on */
                                state = (WinState)i;
                                switchDisplayMode ();
                            }
                        } else {
                            if (toggleState) {
                                /* toggling off */
                                state = WinState.none;
                                switchDisplayMode ();
                            }
                        }
                    }
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    /* check if display Mode changed and sync GUI state */
                    checkDisplayMode ();

                    switch (state) {
                    case WinState.RCS:
                        drawRCSMenu ();
                        break;
                    case WinState.Attitude:
                        drawAttitudeMenu ();
                        break;
                    case WinState.Engine:
                        drawEngineMenu ();
                        break;
                    case WinState.Mass:
                        drawDCoMMenu ();
                        break;
                    case WinState.Debug:
                        drawDebugMenu ();
                        break;
                    }
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
            GUI.DragWindow ();
        }

        void switchDisplayMode ()
        {
            switch(state) {
            case WinState.RCS:
                RCSBuildAid.SetMode(DisplayMode.RCS);
                break;
            case WinState.Attitude:
                RCSBuildAid.SetMode(DisplayMode.Attitude);
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
            case WinState.Mass:
            case WinState.Debug:
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
                case DisplayMode.Attitude:
                    state = WinState.Attitude;
                    break;
                }
                break;
            }
        }

        bool minimizeButton ()
        {
            if (GUI.Button (new Rect (winRect.width - 15, 3, 12, 12), "")) {
                minimized = !minimized;
                minimizedWidth = (int)winRect.width;
                return true;
            }
            return false;
        }

        void drawRCSMenu ()
        {
            MarkerVectors comv = RCSBuildAid.VesselForces;
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                if (RCSBuildAid.RCSlist.Count != 0) {
                    GUILayout.BeginVertical (); 
                    {
                        GUILayout.Label ("Direction:");
                        GUILayout.Label ("Torque:");
                        GUILayout.Label ("Thrust:");
                        if (DeltaV.sanity) {
                            GUILayout.Label ("Delta V:");
                            GUILayout.Label ("Burn time:");
                        }
                    }
                    GUILayout.EndVertical();
                    GUILayout.BeginVertical ();
                    {
                        directionButton();
                        GUILayout.Label(String.Format ("{0:F2} kNm", comv.Torque.magnitude));
                        GUILayout.Label(String.Format ("{0:F2} kN", comv.Thrust.magnitude));
                        if (DeltaV.sanity) {
                            GUILayout.Label(String.Format ("{0:F2} m/s", DeltaV.dV));
                            GUILayout.Label(timeFormat(DeltaV.burnTime));
                        }
                    }
                    GUILayout.EndVertical();
                } else {
                    GUILayout.Label("No RCS thrusters attached", centerText);
                }
            }
            GUILayout.EndHorizontal();
            drawRefButton();
        }

        void drawAttitudeMenu ()
        {
            MarkerVectors comv = RCSBuildAid.VesselForces;
            if (RCSBuildAid.WheelList.Count != 0 || RCSBuildAid.RCSlist.Count != 0) {
                GUILayout.BeginHorizontal (GUI.skin.box);
                {
                    GUILayout.BeginVertical (); 
                    {
                        GUILayout.Label ("Direction:");
                        GUILayout.Label ("Torque:");
                        GUILayout.Label ("Thrust:");
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        directionButton();
                        GUILayout.Label (String.Format ("{0:F2} kNm", comv.Torque.magnitude));
                        GUILayout.Label (String.Format ("{0:F2} kN", comv.Thrust.magnitude));
                    }
                    GUILayout.EndVertical ();
                }
                GUILayout.EndHorizontal ();
                RCSBuildAid.includeWheels = GUILayout.Toggle (RCSBuildAid.includeWheels, "Reaction wheels");
                RCSBuildAid.includeRCS = GUILayout.Toggle (RCSBuildAid.includeRCS, "RCS thrusters");

            } else {
                GUILayout.BeginHorizontal (GUI.skin.box);
                {
                    GUILayout.Label ("No attitude control elements attached", centerText);
                }
                GUILayout.EndHorizontal ();
            }
            drawRefButton ();
        }

        void directionButton()
        {
            if (GUILayout.Button (RCSBuildAid.Direction.ToString (), labelButton)) {
                int i = (int)RCSBuildAid.Direction;
                if (Event.current.button == 0) {
                    i += 1;
                    if (i > 6) {
                        i = 1;
                    }
                } else if (Event.current.button == 1) {
                    i -= 1;
                    if (i < 1) {
                        i = 6;
                    }
                }
                RCSBuildAid.Direction = (RCSBuildAid.Directions)i;
            }
        }

        void drawEngineMenu ()
        {
            MarkerVectors comv = RCSBuildAid.VesselForces;
            MassEditorMarker comm = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ();
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                if (RCSBuildAid.EngineList.Count != 0) {
                    GUILayout.BeginVertical ();
                    {
                        GUILayout.Label ("Torque:");
                        GUILayout.Label ("Thrust:");
                        GUILayout.Label ("TWR:");
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        GUILayout.Label (String.Format ("{0:F2} kNm", comv.Torque.magnitude));
                        GUILayout.Label (String.Format ("{0:F2} kN", comv.Thrust.magnitude));
                        GUILayout.Label (String.Format ("{0:F2}", comv.Thrust.magnitude / (comm.mass * 9.81)));
                    }
                    GUILayout.EndVertical ();
                } else {
                    GUILayout.Label("No engines attached", centerText);
                }
            }
            GUILayout.EndHorizontal();
            drawRefButton();
        }

        void drawDCoMMenu ()
        {
            bool com = RCSBuildAid.showCoM;
            bool dcom = RCSBuildAid.showDCoM;
            bool acom = RCSBuildAid.showACoM;
            Vector3 offset = RCSBuildAid.CoM.transform.position
                - RCSBuildAid.DCoM.transform.position;

            /* data */
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label ("Launch mass:");
                    GUILayout.Label ("Dry mass:");
                    GUILayout.Label ("DCoM offset:");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label (String.Format ("{0:F2} t", CoM_Marker.Mass));
                    GUILayout.Label (String.Format ("{0:F2} t", DCoM_Marker.Mass));
                    GUILayout.Label (String.Format ("{0:F2} m", offset.magnitude));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();

            /* markers toggles */
            GUILayout.BeginVertical (GUI.skin.box);
            {
                GUILayout.BeginHorizontal ();
                {
                    com = GUILayout.Toggle (com, "CoM");
                    dcom = GUILayout.Toggle (dcom, "DCoM");
                    acom = GUILayout.Toggle (acom, "ACoM");
                }
                GUILayout.EndHorizontal ();
                Settings.marker_scale = GUILayout.HorizontalSlider (Settings.marker_scale, 0, 1);
            }
            GUILayout.EndVertical ();

            /* resources */
            GUILayout.BeginVertical ("Resources", GUI.skin.box);
            {
                GUILayout.Space (GUI.skin.box.lineHeight + 4);
                GUILayout.BeginHorizontal ();
                {
                    GUILayout.BeginVertical ();
                    {
                        foreach (string res in DCoM_Marker.Resource.Keys) {
                            DCoM_Marker.resourceCfg [res] = 
                                    GUILayout.Toggle (DCoM_Marker.resourceCfg [res], res);
                        }
                    }
                    GUILayout.EndVertical ();
                    GUILayout.BeginVertical ();
                    {
                        foreach (float mass in DCoM_Marker.Resource.Values) {
                            GUILayout.Label (String.Format ("{0:F2} t", mass));
                        }
                    }
                    GUILayout.EndVertical ();
                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical ();

            RCSBuildAid.showCoM = com;
            RCSBuildAid.showDCoM = dcom;
            RCSBuildAid.showACoM = acom;
            if (!RCSBuildAid.isMarkerVisible (RCSBuildAid.referenceMarker)) {
                selectNextReference ();
            }
        }

        void drawRefButton ()
        {
            if (GUILayout.Button ("Reference: " + RCSBuildAid.referenceMarker)) {
                selectNextReference();
            }
        }

        void selectNextReference ()
        {
            bool[] array = {RCSBuildAid.showCoM, RCSBuildAid.showDCoM, RCSBuildAid.showACoM };
            if (!array.Any (o => o)) {
                return;
            }
            int i = (int)RCSBuildAid.referenceMarker;
            bool found = false;
            for (int j = 0; j < 3; j++) {
                i++;
                if (i == 3) {
                    i = 0;
                }
                if (array[i]) {
                    found = true;
                    break;
                }
            }
            if (found) {
                RCSBuildAid.SetReferenceMarker((CoMReference)i);
            }
        }

        string timeFormat (float seconds)
        {
            int min = (int)seconds / 60;
            int sec = (int)seconds % 60;
            return String.Format("{0:D}m {1:D}s", min, sec);
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
                if (mouseOver && !softLock) {
                    softLock = true;
                    ControlTypes controlTypes = ControlTypes.CAMERACONTROLS 
                                                | ControlTypes.EDITOR_ICON_HOVER 
                                                | ControlTypes.EDITOR_ICON_PICK 
                                                | ControlTypes.EDITOR_PAD_PICK_PLACE 
                                                | ControlTypes.EDITOR_PAD_PICK_COPY 
                                                | ControlTypes.EDITOR_EDIT_STAGES 
                                                | ControlTypes.EDITOR_ROTATE_PARTS 
                                                | ControlTypes.EDITOR_OVERLAYS;

                    InputLockManager.SetControlLock (controlTypes, "RCSBuildAidLock");
                } else if (!mouseOver && softLock) {
                    softLock = false;
                    InputLockManager.RemoveControlLock("RCSBuildAidLock");
                }
            } else if (softLock) {
                softLock = false;
                InputLockManager.RemoveControlLock("RCSBuildAidLock");
            }
        }

        /*
         * Debug stuff
         */

        [Conditional("DEBUG")]
        void debug ()
        {
            if (Input.GetKeyDown(KeyCode.Space)) {
                print (winRect.ToString ());
            }
        }

        [Conditional("DEBUG")]
        void drawDebugMenu ()
        {
            MarkerVectors comv = RCSBuildAid.VesselForces;
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
                    GUILayout.Label (String.Format ("{0:F2} tm²", moi.value));
                    float angAcc = comv.Torque.magnitude / moi.value;
                    GUILayout.Label (String.Format ("{0:F2} r/s²", angAcc));
                    GUILayout.Label (String.Format ("{0:F2} °/s²", angAcc * Mathf.Rad2Deg));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
            DebugSettings.labelMagnitudes = 
                GUILayout.Toggle(DebugSettings.labelMagnitudes, "Show vector magnitudes");
            DebugSettings.inFlightAngularInfo = 
                GUILayout.Toggle(DebugSettings.inFlightAngularInfo, "In flight angular data");
        }
    }
}
