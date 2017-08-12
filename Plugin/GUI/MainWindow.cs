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
        Rect winCBodyListRect;
        bool modeSelect;
        bool softLock;
        bool settings;
        const string title = "RCS Build Aid v0.9.1";

        KeybindConfig pluginShortcut;

        public static bool cBodyListEnabled;
        public static PluginMode cBodyListMode;

        public static Style style;
        public static event Action DrawToggleableContent;
        public static event Action DrawModeContent;

        Dictionary<PluginMode, string> menuTitles = new Dictionary<PluginMode, string> {
            { PluginMode.Attitude, "Attitude"    },
            { PluginMode.RCS     , "Translation" },
            { PluginMode.Engine  , "Engines"     },
        };

        static readonly Dictionary<Direction, string> translationMap = new Dictionary<Direction, string> {
            { Direction.none   , "none"    },
            { Direction.left   , "port"    },
            { Direction.right  , "starboard" },
            { Direction.down   , "ventral" },
            { Direction.up     , "dorsal"  },
            { Direction.forward, "fore"    },
            { Direction.back   , "aft"     },
        };

        static readonly Dictionary<Direction, string> rotationMap = new Dictionary<Direction, string> {
            { Direction.none   , "none"    },
            { Direction.left   , "yaw ←"   },
            { Direction.right  , "yaw →"   },
            { Direction.down   , "pitch ↓" },
            { Direction.up     , "pitch ↑" },
            { Direction.forward, "roll ←"  },
            { Direction.back   , "roll →"  },
        };

        bool minimized { 
            get { return Settings.menu_minimized; }
            set { Settings.menu_minimized = value; }
        }

        int plugin_mode_count {
            get { return Settings.EnabledModes.Count; }
        }

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winRect = new Rect (Settings.window_x, Settings.window_y, Style.main_window_width, Style.main_window_height);
            winCBodyListRect = new Rect ();
            load ();
            gameObject.AddComponent<MenuMass> ();
            gameObject.AddComponent<MenuResources> ();
            gameObject.AddComponent<MenuMarkers> ();
            gameObject.AddComponent<MenuTranslation> ();
            gameObject.AddComponent<MenuEngines> ();
            gameObject.AddComponent<MenuAttitude> ();
            gameObject.AddComponent<MenuParachutes> ();
            Events.ConfigSaving += save;
#if DEBUG
            gameObject.AddComponent<MenuDebug> ();
#endif
        }

        void OnDestroy ()
        {
            Events.ConfigSaving -= save;
        }

        void Start ()
        {
            pluginShortcut = new KeybindConfig (PluginKeys.PLUGIN_TOGGLE);
        }

        void load ()
        {
            /* check if within screen */
            winRect.x = Mathf.Clamp (winRect.x, 0, Screen.width - Style.main_window_width);
            winRect.y = Mathf.Clamp (winRect.y, 0, Screen.height - Style.main_window_height);
        }

        void save ()
        {
            Settings.window_x = (int)winRect.x;
            Settings.window_y = (int)winRect.y;
        }

        void OnGUI ()
        {
            if (style == null) {
                style = new Style ();
            }

            if (RCSBuildAid.Enabled) {
                if (minimized) {
                    winRect.height = Style.main_window_minimized_height;
                    winRect = GUI.Window (winID, winRect, drawWindowMinimized, title, style.mainWindowMinimized);
                } else {
                    if (Event.current.type == EventType.Layout) {
                        winRect.height = Style.main_window_height;
                    }
                    winRect = GUILayout.Window (winID, winRect, drawWindow, title, style.mainWindow);

                    cBodyListEnabled = cBodyListEnabled && (RCSBuildAid.Mode == cBodyListMode);
                    if (cBodyListEnabled) {
                        if (Event.current.type == EventType.Layout) {
                            if ((winRect.x + winRect.width + Style.cbody_list_width + 5) > Screen.width) {
                                winCBodyListRect.x = winRect.x - Style.cbody_list_width - 5;
                            } else {
                                winCBodyListRect.x = winRect.x + winRect.width + 5;
                            }

                            winCBodyListRect.y = winRect.y;
                            winCBodyListRect.width = Style.cbody_list_width;
                            winCBodyListRect.height = Style.main_window_height;
                        }
                        winCBodyListRect = GUILayout.Window (winID + 1, winCBodyListRect, 
                                                             drawBodyListWindow,
                                                             "Celestial bodies", GUI.skin.box);
                    } 
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

        string getModeButtonName (PluginMode mode)
        {
            string buttonName;
            if (!menuTitles.TryGetValue(mode, out buttonName)) {
                buttonName = mode.ToString ();
            }
            return buttonName;
        }

        string getModeButtonName ()
        {
            return getModeButtonName (RCSBuildAid.Mode);
        }

        PluginMode getEnabledPluginMode (int mode) {
            int i = mode % plugin_mode_count;
            if (i < 0) {
                i += plugin_mode_count;
            }
            return Settings.EnabledModes[i];
        }

        int getPluginModeIndex () {
            return Settings.EnabledModes.IndexOf (RCSBuildAid.Mode);
        }

        bool selectModeButton ()
        {
            bool value;
            if (modeSelect) {
                value = GUILayout.Button ("Select mode", style.mainButton);
            } else {
                GUILayout.BeginHorizontal ();
                {
                    nextModeButton ("<", -1);
                    if (RCSBuildAid.Mode == PluginMode.none) {
                        value = GUILayout.Button ("Select mode", style.mainButton);
                    } else {
                        value = GUILayout.Button (getModeButtonName(), style.activeButton);
                    }
                    nextModeButton (">", 1);
                }
                GUILayout.EndHorizontal ();
            }
            return value;
        }

        void nextModeButton(string modeName, int step) {
            if (GUILayout.Button (modeName, style.mainButton, GUILayout.Width (20))) {
                int i = getPluginModeIndex() + step;
                RCSBuildAid.SetMode (getEnabledPluginMode(i));
            }
        }

        void drawWindow (int ID)
        {
            if (minimizeButton () && minimized) {
                return;
            }
            settingsButton ();
            if (settings) {
                drawSettings ();
                GUI.DragWindow ();
                return;
            }
            GUILayout.BeginVertical ();
            {
                if (selectModeButton ()) {
                    modeSelect = !modeSelect;
                }
                if (!modeSelect) {
                    if (DrawModeContent != null) {
                        DrawModeContent ();
                    }
                } else {
                    drawModeSelectList ();
                }

                if (DrawToggleableContent != null) {
                    DrawToggleableContent ();
                }
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        void drawModeSelectList ()
        {
            GUILayout.BeginVertical (GUI.skin.box);
            {
                int r = Mathf.CeilToInt (plugin_mode_count / 2f);
                int i = 0;

                GUILayout.BeginHorizontal ();
                {
                    while (i < plugin_mode_count) {
                        GUILayout.BeginVertical ();
                        {
                            for (int j = 0; (j < r) && (i < plugin_mode_count); j++) {
                                PluginMode mode = getEnabledPluginMode (i);
                                if (GUILayout.Button (getModeButtonName(mode), style.clickLabel)) {
                                    modeSelect = false;
                                    RCSBuildAid.SetMode (mode);
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
                    RCSBuildAid.SetMode (PluginMode.none);
                }
            }
            GUILayout.EndVertical ();
        }

        bool minimizeButton ()
        {
            if (GUI.Button (new Rect (winRect.width - 15, 3, 12, 12), String.Empty, style.tinyButton)) {
                minimized = !minimized;
                return true;
            }
            return false;
        }

        bool settingsButton ()
        {
            if (GUI.Button (new Rect (winRect.width - 30, 3, 12, 12), "s", style.tinyButton)) {
                settings = !settings;
                return true;
            }
            return false;
        }

        void drawSettings ()
        {
            GUILayout.Label ("Settings", style.resourceTableName);
            GUI.enabled = Settings.toolbar_plugin_loaded;
            bool applauncher = Settings.applauncher;
            applauncher = GUILayout.Toggle (applauncher, "Use application launcher");
            if (applauncher != Settings.applauncher) {
                Settings.applauncher = applauncher;
                if (applauncher) {
                    AppLauncher.instance.addButton ();
                } else {
                    AppLauncher.instance.removeButton ();
                    if (!Settings.toolbar_plugin) {
                        Settings.setupToolbar (true);
                    }
                }
            }
            GUI.enabled = Settings.toolbar_plugin_loaded && Settings.applauncher;
            bool toolbar = Settings.toolbar_plugin;
            toolbar = GUILayout.Toggle (toolbar, "Use blizzy's toolbar");
            if (Settings.toolbar_plugin != toolbar) {
                Settings.setupToolbar (toolbar);
            }
            GUI.enabled = true;
            Settings.action_screen = GUILayout.Toggle (Settings.action_screen, "Show in Action Groups");
            Settings.marker_autoscale = GUILayout.Toggle (Settings.marker_autoscale, "Marker autoscaling");
            pluginShortcut.DrawConfig ();
        }

        void drawBodyListWindow (int ID)
        {
            GUILayout.Space(GUI.skin.box.lineHeight + 4);
            GUILayout.BeginVertical ();
            {
                celestialBodyRecurse(Planetarium.fetch.Sun, 5);
            }
            GUILayout.EndVertical();
        }

        void celestialBodyRecurse (CelestialBody body, int padding)
        {
            if ((RCSBuildAid.Mode != PluginMode.Parachutes) || body.atmosphere) {
                style.listButton.padding.left = padding;
                if (GUILayout.Button (body.name, style.listButton)) {
                    cBodyListEnabled = false;
                    Settings.selected_body = body;
                }
            }
            foreach (CelestialBody b in body.orbitingBodies) {
                celestialBodyRecurse(b, padding + 10);
            }
        }

        public static void RotationButtonWithReset()
        {
            GUILayout.BeginHorizontal (); {
                if (GUILayout.Button (rotationMap [RCSBuildAid.Direction], MainWindow.style.smallButton)) {
                    int i = (int)RCSBuildAid.Direction;
                    i = MainWindow.LoopIndexSelect (1, 6, i);
                    RCSBuildAid.SetDirection ((Direction)i);
                }
                if (GUILayout.Button ("R", MainWindow.style.squareButton)) {
                    RCSBuildAid.SetDirection (Direction.none);
                }
            } GUILayout.EndHorizontal ();
        }

        public static void RotationButton()
        {
            if (GUILayout.Button (rotationMap [RCSBuildAid.Direction], MainWindow.style.smallButton)) {
                int i = (int)RCSBuildAid.Direction;
                i = MainWindow.LoopIndexSelect (1, 6, i);
                RCSBuildAid.SetDirection ((Direction)i);
            }
        }

        public static void TranslationButton()
        {
            if (GUILayout.Button (translationMap [RCSBuildAid.Direction], MainWindow.style.smallButton)) {
                int i = (int)RCSBuildAid.Direction;
                i = LoopIndexSelect (1, 6, i);
                RCSBuildAid.SetDirection ((Direction)i);
            }
        }

        public static int LoopIndexSelect(int min_index, int max_index, int i)
        {
            if (Event.current.button == 0) {
                i += 1;
                if (i > max_index) {
                    i = min_index;
                }
            } else if (Event.current.button == 1) {
                i -= 1;
                if (i < min_index) {
                    i = max_index;
                }
            }
            return i;
        }

        public static void ReferenceButton ()
        {
            if (GUILayout.Button (RCSBuildAid.ReferenceType.ToString(), MainWindow.style.smallButton)) {
                selectNextReference ();
            } else if (!MarkerManager.IsMarkerVisible (RCSBuildAid.ReferenceType)) {
                selectNextReference ();
            }
        }

        static void selectNextReference ()
        {
            bool[] array = { 
                MarkerManager.IsMarkerVisible (MarkerType.CoM), 
                MarkerManager.IsMarkerVisible (MarkerType.DCoM),
                MarkerManager.IsMarkerVisible (MarkerType.ACoM)
            };
            if (!array.Any (o => o)) {
                return;
            }
            int i = (int)RCSBuildAid.ReferenceType;
            bool found = false;
            for (int j = 0; j < 3; j++) {
                i = LoopIndexSelect (0, 2, i);
                if (array [i]) {
                    found = true;
                    break;
                }
            }
            if (found) {
                RCSBuildAid.SetReferenceMarker ((MarkerType)i);
            }
        }

        public static string TimeFormat (float seconds)
        {
            int min = (int)seconds / 60;
            int sec = (int)seconds % 60;
            return String.Format("{0:D}m {1:D}s", min, sec);
        }

        bool isMouseOver ()
        {
            var position = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (winRect.Contains (position)) {
                return true;
            }
            return cBodyListEnabled && winCBodyListRect.Contains (position);
        }

        /* Whenever we mouseover our window, we need to lock the editor so we don't pick up
         * parts while dragging the window around */
        void setEditorLock ()
        {
            if (RCSBuildAid.Enabled) {
                bool mouseOver = isMouseOver ();
                if (mouseOver && !softLock) {
                    softLock = true;
                    const ControlTypes controlTypes = ControlTypes.CAMERACONTROLS 
                                                    | ControlTypes.EDITOR_ICON_HOVER 
                                                    | ControlTypes.EDITOR_ICON_PICK 
                                                    | ControlTypes.EDITOR_PAD_PICK_PLACE 
                                                    | ControlTypes.EDITOR_PAD_PICK_COPY 
                                                    | ControlTypes.EDITOR_EDIT_STAGES 
                                                    | ControlTypes.EDITOR_GIZMO_TOOLS
                                                    | ControlTypes.EDITOR_ROOT_REFLOW;

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
                print (winCBodyListRect.ToString ());
            }
        }

    }

    public class KeybindConfig
    {
        KeyBinding key;
        int gui_id = 0;

        protected static int next_gui_id = 1;
        protected static int id_active = 0;

        public KeybindConfig (KeyBinding keybind)
        {
            gui_id = next_gui_id;
            next_gui_id++;
            key = keybind;
        }

        public void DrawConfig ()
        {
            if (gui_id == id_active) {
                if (GUILayout.Button ("Press any key", GUI.skin.button)) {
                    id_active = 0;
                }
                if (Event.current.isKey) {
                    if (Event.current.keyCode == KeyCode.Escape) {
                        id_active = 0;
                        key.primary = new KeyCodeExtended(KeyCode.None);
                    } else if (Event.current.type == EventType.KeyUp) {
                        id_active = 0;
                        key.primary = new KeyCodeExtended(Event.current.keyCode);
                    }
                }
            } else {
                if (GUILayout.Button (string.Format("Shortcut: {0}", key.primary))) {
                    id_active = gui_id;
                }
            }
        }
    }
}
