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

using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.Profiling;

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
        /* PartModules in Vessel */
        static List<PartModule> rcsList;
        static List<PartModule> engineList;
        static List<PartModule> chutesList;
        /* PartModules in cursor */
        static List<PartModule> selectionList;
        /* fields for check for parts quickly */
        static bool hasShipRCS;
        static bool hasSelectedRCS;
        static bool hasShipEngines;
        static bool hasSelectedEngines;
        static bool hasShipChutes;
        static bool hasSelectedChutes;
        
        bool softEnable = true; /* for disabling temporally the plugin */

        /* Properties */

        public static int LastStage {
            get { return StageManager.LastStage; }
        }

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
                switch (HighLogic.LoadedScene) {
                case GameScenes.EDITOR:
                    break;
                default:
                    /* disable during scene changes */
                    return false;
                }

                return userEnable && instance.softEnable;
            }
        }

        /* return ModuleRCS connected to the vessel */
        public static List<PartModule> RCS {
            get {
                if (selectionList.Count == 0) {
                    return rcsList;
                }
                /* there might be RCSs in the cursor */
                var list = getMergedListOf<ModuleRCS>(rcsList);
                return list;
            }
        }

        /* return ModuleRCS that are attached to ShipConstruct */
        public static List<PartModule> ShipRCS {
            get { return rcsList; }
        }
        
        public static bool HasRCS {
            get { return hasShipRCS || hasSelectedRCS; }
        }

        /* return ModuleEngines and MultiModeEngine connected to the vessel */
        public static List<PartModule> Engines {
            get {
                if (selectionList.Count == 0) {
                    return engineList;
                }

                var list = getMergedListOf<ModuleEngines>(engineList);
                return list;
            }
        }

        /* return ModuleEngines that are attached to ShipConstruct */
        public static List<PartModule> ShipEngines {
            get { return engineList; }
        }

        public static bool HasEngines {
            get { return hasShipEngines || hasSelectedEngines; }
        }

        public static List<PartModule> Parachutes {
            get {
                if (selectionList.Count == 0) {
                    return chutesList;
                }

                var list = getMergedListOf<ModuleParachute>(chutesList);
                return list;
            }
        }
        
        public  static bool HasParachutes {
            get { return hasShipChutes || hasSelectedChutes; }
        }

        public static List<PartModule> Selection {
            get { return selectionList; }
        }

        static List<PartModule> getMergedListOf<T>(List<PartModule> list) where T: PartModule
        {
            var list1 = selectionList.OfType<T>().ToList();
            if (list1.Count == 0) {
                return list;
            }
            var list2 = new List<PartModule>(list);
            list2.AddRange(list1);
            return list2;
        }
        
        /* Methods */

        public static void SetReferenceMarker (MarkerType comRef)
        {
            ReferenceType = comRef;
            vesselForces.Marker = GetMarker(ReferenceType);
        }

        public static GameObject GetMarker(MarkerType comRef)
        {
            return MarkerManager.GetMarker (comRef);
        }

        public static void SetMode (PluginMode newMode)
        {
            switch(Mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
            case PluginMode.Engine:
                /* for guessing which mode to enable when using shortcuts (if needed) */
                previousMode = Mode;
                break;
            case PluginMode.Parachutes:
            case PluginMode.none:
                break;
            default:
                /* invalid mode loaded from settings.cfg */
                newMode = PluginMode.none;
                break;
            }

            switch (newMode) {
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

            Mode = newMode;
            Events.OnModeChanged();
        }

        public static void SetIncludeRCS (bool value) {
            if (value != Settings.eng_include_rcs) {
                Settings.eng_include_rcs = value;
                Events.OnModeChanged ();
            }
        }

        public static void SetDirection (Direction newDirection)
        {
            if (Direction == newDirection) {
                return;
            }
            if (Direction != Direction.none) {
                previousDirection = Direction;
            }
            Direction = newDirection;
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
#if DEBUG
            Debug.Log("[RCSBA]: Awake");
#endif
            
            rcsList = new List<PartModule> ();
            engineList = new List<PartModule> ();
            chutesList = new List<PartModule>();
            selectionList = new List<PartModule>();

            events = new Events ();
            events.HookEvents();
            PluginKeys.Setup ();

            gameObject.AddComponent<MarkerManager> ();
            gameObject.AddComponent<MainWindow> ();
            gameObject.AddComponent<DeltaV> ();

            var obj = new GameObject("Vessel Forces Object");
            obj.layer = 2;
            vesselForces = obj.AddComponent<MarkerForces> ();
            
            /* We need these here and not in Start for catch some editor events */
            Events.EditorScreenChanged += onEditorScreenChanged;
            Events.VesselPartChanged += onVesselPartChanged;
            Events.SelectionChanged += onSelectionChange;
            Events.PartDrag += onPartDrag;
            Events.LeavingEditor += onLeavingEditor;
        }

        void Start ()
        {
#if DEBUG
            Debug.Log("[RCSBA]: Start");
#endif
            SetMode(Mode);
            SetDirection (Direction);
        }

        void OnDestroy ()
        {
            events.UnhookEvents ();
            Events.EditorScreenChanged -= onEditorScreenChanged;
            Events.VesselPartChanged -= onVesselPartChanged;
            Events.SelectionChanged -= onSelectionChange;
            Events.PartDrag -= onPartDrag;
            Events.LeavingEditor -= onLeavingEditor;
        }

        void onLeavingEditor()
        {
            ModuleForces.ClearLists();
        }

        void onEditorScreenChanged (EditorScreen screen) {
            /* the plugin isn't useful in all the editor screens */
            if (EditorScreen.Parts == screen) {
                setSoftActive (true);
            } else if (Settings.action_screen && (EditorScreen.Actions == screen)) {
                setSoftActive (true);
            } else if (Settings.crew_screen && (EditorScreen.Crew == screen)) {
                setSoftActive (true);
            } else {
                setSoftActive (false);
            }
        }

        void onVesselPartChanged()
        {
            /*
             * Update our lists about the parts in the ship
             *
             * These parts should always get a ModuleForce component.
             * ModuleForces also check these list for enable or disable
             * themselves.
             */
            
            updateModuleLists();
            /* add our force MonoBehaviours as needed */
            addForces();
        }

        void onSelectionChange()
        {
            /*
             * Parts picked by the cursor should have a ModuleForce component
             * added. No lists are updated here.
             */
            
            addForcesSelection();
        }

        void onPartDrag()
        {
            /*
             * Update list of parts selected but "connected" to the ship
             *
             * ModuleForces check these lists to see if the part is connected
             * to the vessel and if they should enable or disable themselves,
             * this mostly makes sense when the player is dragging parts over
             * the surface of the ship.
             */
            
            updateSelectedModuleLists();
        }

        void Update ()
        {
            Profiler.BeginSample("[RCSBA] RCSBuildAid Update");
            if (PluginKeys.PLUGIN_TOGGLE.GetKeyDown() && !EditorUtils.isInputFieldFocused()) {
                SetActive (!Enabled);
            }

            if (Enabled) {
                /* Switching direction */
                if (Input.anyKeyDown && !EditorUtils.isInputFieldFocused()) {
                    if (PluginKeys.TRANSLATE_UP.GetKey ()) {
                        switchDirection (Direction.up);
                    } else if (PluginKeys.TRANSLATE_DOWN.GetKey ()) {
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
            Profiler.EndSample();
        }

        void updateModuleLists ()
        {
            Profiler.BeginSample("[RCSBA] RCSBuildAid updateModuleList");
            if (EditorLogic.RootPart == null) {
                rcsList.Clear();
                engineList.Clear();
                chutesList.Clear();
                hasShipChutes = false;
                hasShipRCS = false;
                hasShipEngines = false;
                Profiler.EndSample();
                return;
            }
            rcsList = EditorUtils.GetModulesOf<ModuleRCS> ();
            chutesList = EditorUtils.GetModulesOf<ModuleParachute> ();
            var moduleEngineList = EditorUtils.GetModulesOf<ModuleEngines> ();
            var multiModeEngineList = EditorUtils.GetModulesOf<MultiModeEngine> ();
            engineList = sortEngineList(moduleEngineList, multiModeEngineList);
            hasShipRCS = rcsList.Count > 0;
            hasShipEngines = engineList.Count > 0;
            hasShipChutes = chutesList.Count > 0;
            Profiler.EndSample();
        }
        
        void updateSelectedModuleLists()
        {
            Profiler.BeginSample("[RCSBA] RCSBuildAid updateSelectedModuleList");
            if (EditorLogic.SelectedPart == null) {
                selectionList.Clear();
                hasSelectedRCS = false;
                hasSelectedEngines = false;
                hasSelectedChutes = false;
                Profiler.EndSample();
                return;
            }
            selectionList = new List<PartModule>();
            var list = EditorUtils.GetSelectedModulesOf<ModuleRCS>();
            hasSelectedRCS = list.Count > 0;
            selectionList.AddRange(list);
            list = EditorUtils.GetSelectedModulesOf<ModuleParachute>();
            hasSelectedChutes = list.Count > 0;
            selectionList.AddRange(list);
            var moduleEngineList = EditorUtils.GetSelectedModulesOf<ModuleEngines> ();
            var multiModeEngineList = EditorUtils.GetSelectedModulesOf<MultiModeEngine> ();
            list = sortEngineList(moduleEngineList, multiModeEngineList);
            hasSelectedEngines = list.Count > 0;
            selectionList.AddRange(list);
            Profiler.EndSample();
        }
        
        List<PartModule> sortEngineList(List<PartModule> moduleEngineList, List<PartModule> multiModeEngineList)
        {
            Profiler.BeginSample("[RCSBA] RCSBuildAid sortEngineList");
            var list = new List<PartModule>();
            /* don't add engines that are using MultiModeEngine */
            foreach (PartModule eng in moduleEngineList) {
                bool found = false;
                foreach (PartModule multi in multiModeEngineList) {
                    if (multi.part == eng.part) {
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    list.Add (eng);
                }
            }
            list.AddRange(multiModeEngineList);
            Profiler.EndSample();
            return list;
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
            /* add force MonoBehaviours to parts in vessel */
            Profiler.BeginSample("[RCSBA] RCSBuildAid addForces");
            foreach (var mod in rcsList) {
                ModuleForces.Add<RCSForce> (mod);
            }
            foreach (var mod in engineList) {
                if (mod is ModuleEngines) {
                    ModuleForces.Add<EngineForce>(mod);
                } else if (mod is MultiModeEngine) {
                    ModuleForces.Add<MultiModeEngineForce> (mod);
                }
            }
            Profiler.EndSample();
        }

        void addForcesSelection()
        {
            /* add force MonoBehaviours to parts grabbed by the cursor */
            Profiler.BeginSample("[RCSBA] RCSBuildAid addForcesSelection");
            if (EditorLogic.SelectedPart == null) {
                Profiler.EndSample();
                return;
            }
            const bool onlyConnected = false;
            var list = EditorUtils.GetSelectedModulesOf<ModuleRCS>(onlyConnected);
            foreach (var pm in list) {
                ModuleForces.Add<RCSForce>(pm);
            }
            var moduleEngineList = EditorUtils.GetSelectedModulesOf<ModuleEngines>(onlyConnected);
            var multiModeEngineList = EditorUtils.GetSelectedModulesOf<MultiModeEngine>(onlyConnected);
            list = sortEngineList(moduleEngineList, multiModeEngineList);
            foreach (var pm in list) {
                if (pm is MultiModeEngine) {
                    ModuleForces.Add<MultiModeEngineForce>(pm);
                } else if (pm is ModuleEngines) {
                    ModuleForces.Add<EngineForce>(pm);
                }
            }
            Profiler.EndSample();
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
                    if (!HasRCS) {
                        ScreenMessages.PostScreenMessage("No RCS thrusters in place.", 3, 
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                case PluginMode.Attitude:
                    if (!HasRCS) {
                        ScreenMessages.PostScreenMessage("No attitude control elements in place.", 3,
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                }
            }
        }
	}
}
