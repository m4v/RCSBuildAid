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
using UnityEngine;

namespace RCSBuildAid
{
    public enum PluginMode { none, RCS, Attitude, Engine, Parachutes };
    public enum Direction { none, right, left, up, down, forward, back };

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RCSBuildAid : MonoBehaviour
    {
        /* Fields */
        public static RCSBuildAid instance { get; private set; }

        static Events events;
        static MarkerForces vesselForces;
        static bool userEnable;
        static PluginMode previousMode = PluginMode.RCS;
        static Direction previousDirection = Direction.right;
        static List<PartModule> rcsList;
        static List<PartModule> engineList;
        static List<PartModule> chutesList;

        bool softEnable = true; /* for disabling temporally the plugin */

        /* Properties */

        public static int LastStage { get; private set; }
        public static GameObject CoM { 
            get { return MarkerManager.CoM; }
        }
        public static GameObject DCoM {
            get { return MarkerManager.DCoM; }
        }
        public static GameObject ACoM {
            get { return MarkerManager.ACoM; }
        }
        public static GameObject CoD {
            get { return MarkerManager.CoD; }
        }
        /* NOTE directions are reversed because they're the direction of the exhaust and not movement */
        public static Vector3 TranslationVector {
            get {
                if (ReferenceTransform == null) {
                    return Vector3.zero;
                }
                switch (Direction) {
                case Direction.forward:
                    return ReferenceTransform.up * -1;
                case Direction.back:
                    return ReferenceTransform.up;
                case Direction.right:
                    return ReferenceTransform.right * -1;
                case Direction.left:
                    return ReferenceTransform.right;
                case Direction.up:
                    return ReferenceTransform.forward;
                case Direction.down:
                    return ReferenceTransform.forward * -1;
                default:
                    return Vector3.zero;
                }
            }
        }

        /* for rotation: return rotation axis for roll, pitch and yaw */
        public static Vector3 RotationVector {
            get {
                if (ReferenceTransform == null) {
                    return Vector3.zero;
                }
                switch (Direction) {
                case Direction.forward:
                    /* roll left */
                    return ReferenceTransform.up * -1;
                case Direction.back:
                    /* roll right */
                    return ReferenceTransform.up;
                case Direction.right:
                    /* yaw right */
                    return ReferenceTransform.forward;
                case Direction.left:
                    /* yaw left */
                    return ReferenceTransform.forward * -1;
                case Direction.up:
                    /* pitch up */
                    return ReferenceTransform.right;
                case Direction.down:
                    /* pitch down */
                    return ReferenceTransform.right * -1;
                default:
                    return Vector3.zero;
                }
            }
        }

        public static Transform ReferenceTransform { 
            get { 
                if (EditorLogic.RootPart != null) {
                    return EditorLogic.RootPart.GetReferenceTransform ();
                }
                return null;
            }
        }

        public static MarkerType ReferenceType { 
            get { return Settings.com_reference; }
            private set { Settings.com_reference = value; }
        }

        public static PluginMode Mode {
            get { return Settings.plugin_mode; }
            private set { Settings.plugin_mode = value; }
        }

        public static bool IncludeRCS {
            get { return Settings.eng_include_rcs; }
        }

        public static GameObject ReferenceMarker {
            get { return GetMarker (ReferenceType); }
        }

        public static MarkerForces VesselForces {
            get { return vesselForces; }
        }

        public static Direction Direction { 
            get { return Settings.direction; }
            private set { Settings.direction = value; }
        }

        public static bool Enabled {
            get { 
                if (EditorLogic.fetch == null) {
                    return false;
                }

                switch (HighLogic.LoadedScene) {
                case GameScenes.EDITOR:
                    break;
                default:
                    /* disable during scene changes */
                    return false;
                }

                return userEnable && (instance != null && instance.softEnable);
            }
        }

        public static List<PartModule> RCS {
            get { return rcsList; }
        }

        public static List<PartModule> Engines {
            get { return engineList; }
        }

        public static List<PartModule> Parachutes {
            get { return chutesList; }
        }

        /* Methods */

        public static void SetReferenceMarker (MarkerType comref)
        {
            ReferenceType = comref;
            vesselForces.Marker = GetMarker(ReferenceType);
        }

        public static GameObject GetMarker(MarkerType comref)
        {
            return MarkerManager.GetMarker (comref);
        }

        public static void SetMode (PluginMode new_mode)
        {
            switch(Mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
            case PluginMode.Engine:
                /* for guesssing which mode to enable when using shortcuts (if needed) */
                previousMode = Mode;
                break;
            case PluginMode.Parachutes:
            case PluginMode.none:
                break;
            default:
                /* invalid mode loaded from settings.cfg */
                new_mode = PluginMode.none;
                break;
            }

            switch (new_mode) {
            case PluginMode.Engine:
                /* reset gimbals if we're switching to engines */
                SetDirection (Direction.none);
                break;
            case PluginMode.Attitude:
            case PluginMode.RCS:
                /* these modes should always have a direction */
                SetDirection (previousDirection);
                break;
            }

            Mode = new_mode;
            Events.OnModeChanged();
        }

        public static void SetIncludeRCS (bool value) {
            if (value != Settings.eng_include_rcs) {
                Settings.eng_include_rcs = value;
                Events.OnModeChanged ();
            }
        }

        public static void SetDirection (Direction new_direction)
        {
            if (Direction == new_direction) {
                return;
            }
            if (Direction != Direction.none) {
                previousDirection = Direction;
            }
            Direction = new_direction;
            Events.OnDirectionChanged ();
        }

        void setPreviousMode ()
        {
            SetMode (previousMode);
        }

        public static void SetActive (bool value)
        {
            userEnable = value;
            CoM.SetActive (value);
            DCoM.SetActive (value);
            ACoM.SetActive (value);

            if (value) {
                Events.OnPluginEnabled (true);
            } else {
                Events.OnPluginDisabled (true);
            }
            Events.OnPluginToggled (value, true);
        }

        void setSoftActive (bool value)
        {
            /* for disable the plugin temporally without changing what the user set */
            softEnable = value;
            bool pluginEnabled = Enabled;
            CoM.SetActive (pluginEnabled);
            DCoM.SetActive (pluginEnabled);
            ACoM.SetActive (pluginEnabled);
            if (pluginEnabled) {
                Events.OnPluginEnabled (false);
            } else {
                Events.OnPluginDisabled (false);
            }
            Events.OnPluginToggled (value, false);
        }
        
        public RCSBuildAid ()
        {
            instance = this;
        }

        void Awake ()
        {
            rcsList = new List<PartModule> ();
            engineList = new List<PartModule> ();

            events = new Events ();
            events.HookEvents();
            PluginKeys.Setup ();

            gameObject.AddComponent<MarkerManager> ();
            gameObject.AddComponent<MainWindow> ();
            gameObject.AddComponent<DeltaV> ();

            var obj = new GameObject("Vessel Forces Object");
            obj.layer = 2;
            vesselForces = obj.AddComponent<MarkerForces> ();

            Events.EditorScreenChanged += onEditorScreenChanged;
        }

        void OnDestroy ()
        {
            events.UnhookEvents ();
            Events.EditorScreenChanged -= onEditorScreenChanged;
        }

        void onEditorScreenChanged (EditorScreen screen) {
            /* the plugin isn't useful in all the editor screens */
            if (EditorScreen.Parts == screen) {
                setSoftActive (true);
            } else if (Settings.action_screen && (EditorScreen.Actions == screen)) {
                setSoftActive (true);
            } else {
                setSoftActive (false);
            }
        }

        void Start ()
        {
            SetMode(Mode);
            SetDirection (Direction);
        }

        void Update ()
        {
            bool disableShortcuts = EditorUtils.isInputFieldFocused ();

            if (!disableShortcuts && PluginKeys.PLUGIN_TOGGLE.GetKeyDown()) {
                SetActive (!Enabled);
            }

            if (Enabled) {
                bool b = (Mode == PluginMode.Parachutes);
                if (CoD.activeInHierarchy != b) {
                    CoD.SetActive(b);
                }
            } else if (CoD.activeInHierarchy) {
                CoD.SetActive(false);
            }

            if (Enabled) {
                updateModuleLists ();
                addForces ();
                //EditorUtils.RunOnAllParts (addDragVectors);

                /* find the bottommost stage with engines */
                int stage = 0;
                foreach (PartModule mod in engineList) {
                    if (mod.part.inverseStage > stage) {
                        stage = mod.part.inverseStage;
                    }
                }
                LastStage = stage;

                /* Switching direction */
                if (!disableShortcuts && Input.anyKeyDown) {
                    if (PluginKeys.TRANSLATE_UP.GetKeyDown ()) {
                        switchDirection (Direction.up);
                    } else if (PluginKeys.TRANSLATE_DOWN.GetKeyDown ()) {
                        switchDirection (Direction.down);
                    } else if (PluginKeys.TRANSLATE_FWD.GetKeyDown ()) {
                        switchDirection (Direction.forward);
                    } else if (PluginKeys.TRANSLATE_BACK.GetKeyDown ()) {
                        switchDirection (Direction.back);
                    } else if (PluginKeys.TRANSLATE_LEFT.GetKeyDown ()) {
                        switchDirection (Direction.left);
                    } else if (PluginKeys.TRANSLATE_RIGHT.GetKeyDown ()) {
                        switchDirection (Direction.right);
                    }
                }
            }
        }

        void updateModuleLists ()
        {
            rcsList = EditorUtils.GetModulesOf<ModuleRCS> ();
            chutesList = EditorUtils.GetModulesOf<ModuleParachute> ();
            engineList.Clear ();

            var tempEngineList = EditorUtils.GetModulesOf<ModuleEngines> ();
            var multiModeList = EditorUtils.GetModulesOf<MultiModeEngine> ();

            /* dont add engines that are using MultiModeEngine */
            foreach (PartModule mod in tempEngineList) {
                bool found = false;
                foreach (PartModule mod2 in multiModeList) {
                    if (mod2.part == mod.part) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    engineList.Add (mod);
                }
            }
            engineList.AddRange(multiModeList);
        }

        void addDragVectors(Part part) {
            var dragVector = part.GetComponent<DragCubeVector> ();
            if (dragVector == null) {
                dragVector = part.gameObject.AddComponent<DragCubeVector> ();
                dragVector.part = part;
            }
        }

        void addForces ()
        {
            foreach (var mod in rcsList) {
                addForce<RCSForce> (mod);
            }
            foreach (var mod in engineList) {
                if (mod is ModuleEngines) {
                    addForce<EngineForce> (mod);
                } else if (mod is MultiModeEngine) {
                    addForce<MultiModeEngineForce> (mod);
                }
            }
        }

        void addForce<T> (PartModule module) where T: ModuleForces
        {
            T force = module.GetComponent<T> ();
            if (force == null) {
                module.gameObject.AddComponent<T> ();
            }
        }

        void switchDirection (Direction dir)
        {
            if (Mode != PluginMode.RCS && Mode != PluginMode.Attitude && Mode != PluginMode.Engine) {
                /* directions only make sense in some modes, so lets enable the last one used. */
                setPreviousMode();
                if (Direction == dir) {
                    /* don't disable in this case */
                    return;
                }
            }
            if (Direction == dir) {
                /* disabling due to pressing twice the same key */
                SetDirection(Direction.none);
                if (Mode != PluginMode.Engine) {
                    SetMode (PluginMode.none);
                }
            } else {
                /* enabling RCS vectors or switching direction */
                if (Mode == PluginMode.none) {
                    setPreviousMode();
                }
                SetDirection(dir);
                switch(Mode) {
                case PluginMode.RCS:
                    if (RCS.Count == 0) {
                        ScreenMessages.PostScreenMessage(
                            "No RCS thrusters in place.", 3,
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                case PluginMode.Attitude:
                    if (RCS.Count == 0) {
                        ScreenMessages.PostScreenMessage(
                            "No attitude control elements in place.", 3,
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                }
            }
        }
	}
}
