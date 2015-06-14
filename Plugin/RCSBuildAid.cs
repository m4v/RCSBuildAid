/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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
    public enum MarkerType { CoM, DCoM, ACoM };
    public enum PluginMode { none, RCS, Attitude, Engine };
    public enum Direction { none, right, left, up, down, forward, back };

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RCSBuildAid : MonoBehaviour
    {
        /* Fields */
        public static Events events;
        public static RCSBuildAid instance;

        static MarkerForces vesselForces;
        static bool userEnable;
        static PluginMode previousMode = PluginMode.RCS;
        static Direction previousDirection = Direction.right;
        static Dictionary<MarkerType, GameObject> referenceDict = 
            new Dictionary<MarkerType, GameObject> ();
        static List<PartModule> rcsList;
        static List<PartModule> engineList;

        EditorVesselOverlays vesselOverlays;
        bool disableShortcuts;
        bool softEnable = true;

        /* Properties */

        public static int LastStage { get; private set; }
        public static GameObject CoM { get; private set; }
        public static GameObject DCoM { get; private set; }
        public static GameObject ACoM { get; private set; }

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

        /* for rotation: roll, pitch and yaw */
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
                    return ReferenceTransform.forward;
                case Direction.left:
                    return ReferenceTransform.forward * -1;
                case Direction.up:
                    return ReferenceTransform.right;
                case Direction.down:
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
                return CheckEnabledConditions() && userEnable && (instance != null && instance.softEnable);
            }
        }

        public static List<PartModule> RCS {
            get { return rcsList; }
        }

        public static List<PartModule> Engines {
            get { return engineList; }
        }

        /* Methods */

        [Obsolete]
        public static bool CheckEnabledConditions ()
        {
            switch (HighLogic.LoadedScene) {
            case GameScenes.EDITOR:
                break;
            default:
                /* disable during scene changes */
                return false;
            }

            /* the plugin isn't useful in all the editor screens */
            if (EditorLogic.fetch.editorScreen == EditorScreen.Parts) {
                return true;
            } 
            if (Settings.action_screen && (EditorLogic.fetch.editorScreen == EditorScreen.Actions)) {
                return true;
            }
            return false;
        }

        public static void SetReferenceMarker (MarkerType comref)
        {
            ReferenceType = comref;
            vesselForces.Marker = GetMarker(ReferenceType);
        }

        public static GameObject GetMarker (MarkerType comref)
        {
            return referenceDict [comref];
        }

        public static void SetMarkerVisibility (MarkerType marker, bool value)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            if (value) {
                markerVis.Show ();
            } else {
                markerVis.RCSBAToggle = false;
            }
            switch (marker) {
            case MarkerType.CoM:
                Settings.show_marker_com = value;
                break;
            case MarkerType.DCoM:
                Settings.show_marker_dcom = value;
                break;
            case MarkerType.ACoM:
                Settings.show_marker_acom = value;
                break;
            }
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
            events.OnModeChanged();
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
            events.OnDirectionChanged ();
        }

        void setPreviousMode ()
        {
            SetMode (previousMode);
        }

        public static void SetActive (bool enabled)
        {
            userEnable = enabled;
            CoM.SetActive (enabled);
            DCoM.SetActive (enabled);
            ACoM.SetActive (enabled);

            if (enabled) {
                events.OnPluginEnabled ();
            } else {
                events.OnPluginDisabled ();
            }
        }

        void setSoftActive (bool enabled)
        {
            /* for disable the plugin temporally without changing what the user set */
            softEnable = enabled;
            bool pluginEnabled = Enabled;
            CoM.SetActive (pluginEnabled);
            DCoM.SetActive (pluginEnabled);
            ACoM.SetActive (pluginEnabled);
            if (pluginEnabled) {
                events.OnPluginEnabled ();
            } else {
                events.OnPluginDisabled ();
            }
        }
        
        public static bool IsMarkerVisible (MarkerType marker)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            return markerVis.isVisible;
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
            events.HookEvents ();
            PluginKeys.Setup ();

            gameObject.AddComponent<MainWindow> ();
            gameObject.AddComponent<DeltaV> ();
            // Analysis disable once AccessToStaticMemberViaDerivedType
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType(
                typeof(EditorVesselOverlays));

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
            setupMarker (); /* must be in Start because CoMmarker is null in Awake */
            SetMode(Mode);
            SetDirection (Direction);

            /* enable markers if plugin starts active */
            SetActive(userEnable);
        }

        void comButtonClick ()
        {
            bool markerEnabled = !CoM.activeInHierarchy;
            if (userEnable) {
                bool visible = !CoM.GetComponent<MarkerVisibility> ().CoMToggle;
                CoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                DCoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                ACoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                /* we need the CoM to remain active, but we can't stop the editor from
                 * deactivating it when the CoM toggle button is used, so we toggle it now so is
                 * toggled again by the editor. That way it will remain active. */
                CoM.SetActive(markerEnabled);
            }

            if (!userEnable && markerEnabled) {
                /* restore CoM visibility, so the regular CoM toggle button works. */
                var markerVisibility = CoM.GetComponent<MarkerVisibility> ();
                if (markerVisibility != null) {
                    markerVisibility.Show ();
                }
            }
        }

        void setupMarker ()
        {
            /* get CoM */
            if (vesselOverlays.CoMmarker == null) {
                gameObject.SetActive(false);
                throw new Exception("CoM marker is null, this shouldn't happen.");
            }
            CoM = vesselOverlays.CoMmarker.gameObject;

            /* init DCoM */
            DCoM = (GameObject)UnityEngine.Object.Instantiate (CoM);
            Destroy (DCoM.GetComponent<EditorMarker_CoM> ());           /* we don't need this */
            DCoM.name = "DCoM Marker";
            if (DCoM.transform.childCount > 0) {
                /* Stock CoM doesn't have any attached objects, if there's some it means
                 * there's a plugin doing the same thing as us. We don't want extra
                 * objects */
                for (int i = 0; i < DCoM.transform.childCount; i++) {
                    Destroy (DCoM.transform.GetChild (i).gameObject);
                }
            }

            /* init ACoM */
            ACoM = (GameObject)UnityEngine.Object.Instantiate(DCoM);
            ACoM.name = "ACoM Marker";

            referenceDict[MarkerType.CoM] = CoM;
            referenceDict[MarkerType.DCoM] = DCoM;
            referenceDict[MarkerType.ACoM] = ACoM;

            /* CoM setup, replace stock component with our own */
            CoMMarker comMarker = CoM.AddComponent<CoMMarker> ();
            comMarker.posMarkerObject = vesselOverlays.CoMmarker.posMarkerObject;
            Destroy (vesselOverlays.CoMmarker);
            vesselOverlays.CoMmarker = comMarker;

            /* setup DCoM */
            DCoMMarker dcomMarker = DCoM.AddComponent<DCoMMarker> (); /* we do need this    */
            dcomMarker.posMarkerObject = DCoM;

            /* setup ACoM */
            var acomMarker = ACoM.AddComponent<AverageMarker> ();
            acomMarker.posMarkerObject = ACoM;
            acomMarker.CoM1 = comMarker;
            acomMarker.CoM2 = dcomMarker;

            var obj = new GameObject("Vessel Forces Object");
            obj.layer = CoM.layer;
            vesselForces = obj.AddComponent<MarkerForces> ();
            SetReferenceMarker(ReferenceType);

            /* scaling for CoL and CoT markers */
            vesselOverlays.CoLmarker.gameObject.AddComponent<MarkerScaler> ();
            vesselOverlays.CoTmarker.gameObject.AddComponent<MarkerScaler> ();

            /* attach our method to the CoM toggle button */
            vesselOverlays.toggleCoMbtn.AddValueChangedDelegate(delegate { comButtonClick(); });
        }

        void Update ()
        {
            disableShortcuts = EditorLogic.fetch.MouseOverTextFields ();
            if (!disableShortcuts && PluginKeys.PLUGIN_TOGGLE.GetKeyDown()) {
                SetActive (!Enabled);
            }

            if (Enabled) {
                updateModuleLists ();
                addForces ();

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
