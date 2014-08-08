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
    public class MainWindow : MonoBehaviour
    {
        int winID;
        Rect winRect;
        bool modeSelect = false;
        bool softLock = false;
        string title = "RCS Build Aid v0.5";
        int winX = 270, winY = 50;
        int minWidth = 184;
        int maxWidth = 184;
        int minHeight = 102;
        int maxHeight = 102;
        int minimizedWidth = 184;
        int minimizedHeight = 26;

        public static Style style;
        public static event Action onDrawToggleableContent;
        public static event Action onDrawModeContent;

        Dictionary<PluginMode, string> menuTitles = new Dictionary<PluginMode, string> () {
            { PluginMode.Attitude, "Attitude"    },
            { PluginMode.RCS     , "Translation" },
            { PluginMode.Engine  , "Engines"     },
        };

        bool minimized { 
            get { return Settings.menu_minimized; }
            set { Settings.menu_minimized = value; }
        }

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (winX, winY, minWidth, minHeight);
            Load ();
            onDrawModeContent = null;
            onDrawToggleableContent = null;
            onDrawToggleableContent += gameObject.AddComponent<MenuMass> ().DrawContent;
            onDrawToggleableContent += gameObject.AddComponent<MenuResources> ().DrawContent;
            onDrawToggleableContent += gameObject.AddComponent<MenuMarkers> ().DrawContent;
            RCSBuildAid.events.onModeChange += MenuTranslation.onModeChange;
            RCSBuildAid.events.onModeChange += MenuAttitude.onModeChange;
            RCSBuildAid.events.onModeChange += MenuEngines.onModeChange;
#if DEBUG
            onDrawToggleableContent += gameObject.AddComponent<MenuDebug> ().DrawContent;
#endif
        }

        void Start ()
        {
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Load ()
        {
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
        }

        void OnGUI ()
        {
            switch (HighLogic.LoadedScene) {
            case GameScenes.EDITOR:
            case GameScenes.SPH:
                break;
            default:
                /* don't show window during scene changes */
                return;
            }

            if (style == null) {
                style = new Style ();
            }

            if (RCSBuildAid.Enabled) {
                if (minimized) {
                    GUI.skin.window.clipping = TextClipping.Overflow;
                    winRect.height = minimizedHeight;
                    winRect.width = minimizedWidth;
                    winRect = GUI.Window (winID, winRect, drawWindowMinimized, title);
                } else {
                    GUI.skin.window.clipping = TextClipping.Clip;
                    if (Event.current.type == EventType.Layout) {
                        winRect.height = minHeight;
                        winRect.width = minWidth;
                    }
                    winRect = GUILayout.Window (winID, winRect, drawWindow, title);
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

        bool selectModeButton ()
        {
            if (modeSelect || (RCSBuildAid.mode == PluginMode.none)) {
                return GUILayout.Button ("Select mode", style.mainButton);
            } else {
                return GUILayout.Button (menuTitles [RCSBuildAid.mode], style.activeButton);
            }
        }

        void drawWindow (int ID)
        {
            if (minimizeButton () && minimized) {
                return;
            }
            GUILayout.BeginVertical ();
            {
                if (selectModeButton ()) {
                    modeSelect = !modeSelect;
                }
                if (!modeSelect) {
                    if (onDrawModeContent != null) {
                        onDrawModeContent ();
                    }
                } else {
                    drawModeSelectList ();
                }

                if (onDrawToggleableContent != null) {
                    onDrawToggleableContent ();
                }
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        void drawModeSelectList ()
        {
            GUILayout.BeginVertical (GUI.skin.box);
            {
                int n = 3; /* total number of modes */
                int r = Mathf.CeilToInt (n / 2f);
                int i = 1;

                GUILayout.BeginHorizontal ();
                {
                    while (i <= n) {
                        GUILayout.BeginVertical ();
                        {
                            for (int j = 0; (j < r) && (i <= n); j++) {
                                if (GUILayout.Button (menuTitles [(PluginMode)i], style.clickLabel)) {
                                    modeSelect = false;
                                    RCSBuildAid.events.SetMode ((PluginMode)i);
                                }
                                i++;
                            }
                        }
                        GUILayout.EndVertical ();
                    }
                }
                GUILayout.EndHorizontal ();
                if (GUILayout.Button ("None", style.clickLabelCenter)) {
                    modeSelect = false;
                    RCSBuildAid.events.SetMode (PluginMode.none);
                }
            }
            GUILayout.EndVertical ();
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

        public static void directionButton()
        {
            if (GUILayout.Button (RCSBuildAid.Direction.ToString (), MainWindow.style.smallButton)) {
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

        public static void referenceButton ()
        {
            if (GUILayout.Button (RCSBuildAid.referenceMarker.ToString(), MainWindow.style.smallButton)) {
                selectNextReference ();
            } else if (!RCSBuildAid.isMarkerVisible (RCSBuildAid.referenceMarker)) {
                selectNextReference ();
            }
        }

        static void selectNextReference ()
        {
            bool[] array = { 
                RCSBuildAid.isMarkerVisible (MarkerType.CoM), 
                RCSBuildAid.isMarkerVisible (MarkerType.DCoM),
                RCSBuildAid.isMarkerVisible (MarkerType.ACoM)
            };
            if (!array.Any (o => o)) {
                return;
            }
            int i = (int)RCSBuildAid.referenceMarker;
            bool found = false;
            for (int j = 0; j < 3; j++) {
                if (Event.current.button == 1) {
                    i -= 1;
                    if (i < 0) {
                        i = 2;
                    }
                } else {
                    i += 1;
                    if (i > 2) {
                        i = 0;
                    }
                }
                if (array [i]) {
                    found = true;
                    break;
                }
            }
            if (found) {
                RCSBuildAid.SetReferenceMarker ((MarkerType)i);
            }
        }

        public static string timeFormat (float seconds)
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
                    ControlTypes controlTypes =   ControlTypes.CAMERACONTROLS 
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

    }
}
