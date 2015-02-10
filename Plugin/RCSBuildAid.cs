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

        static MarkerForces vesselForces;
        static bool pluginEnabled;
        static Dictionary<MarkerType, GameObject> referenceDict = 
            new Dictionary<MarkerType, GameObject> ();
        static List<PartModule> rcsList;
        static List<PartModule> engineList;

        EditorVesselOverlays vesselOverlays;
        bool disableShortcuts;

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
                switch (events.direction) {
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
                switch (events.direction) {
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

        public static Transform ReferenceTransform { get; private set; }

        public static MarkerType ReferenceType { 
            get { return Settings.com_reference; }
            private set { Settings.com_reference = value; }
        }

        public static PluginMode Mode { 
            get { return events.mode; }
            set { events.SetMode (value); }
        }

        public static GameObject ReferenceMarker {
            get { return GetMarker (ReferenceType); }
        }

        public static MarkerForces VesselForces {
            get { return vesselForces; }
        }

        public static Direction Direction {
            get { return events.direction; }
            set { events.SetDirection(value); }
        }

        public static bool Enabled {
            get { 
                if (EditorLogic.fetch == null) {
                    return false;
                }
                return CheckEditorScreen () && pluginEnabled;
            }
            set { 
                pluginEnabled = value;
                CoM.SetActive (value);
                DCoM.SetActive (value);
                ACoM.SetActive (value);
            }
        }

        public static List<PartModule> RCS {
            get { return rcsList; }
        }

        public static List<PartModule> Engines {
            get { return engineList; }
        }

        /* Methods */

        public static bool CheckEditorScreen ()
        {
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

        public static bool IsMarkerVisible (MarkerType marker)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            return markerVis.isVisible;
        }

        void Awake ()
        {
            rcsList = new List<PartModule> ();
            engineList = new List<PartModule> ();

            events = new Events ();

            gameObject.AddComponent<MainWindow> ();
            gameObject.AddComponent<DeltaV> ();
            // Analysis disable once AccessToStaticMemberViaDerivedType
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType(
                typeof(EditorVesselOverlays));

            GameEvents.onEditorShipModified.Add(onShipModified);
        }

        void OnDestroy ()
        {
            GameEvents.onEditorShipModified.Remove(onShipModified);
        }

        void Start ()
        {
            setupMarker (); /* must be in Start because CoMmarker is null in Awake */
            events.SetMode(events.mode);
            events.SetDirection (events.direction);

            /* enable markers if plugin starts active */
            Enabled = pluginEnabled;
        }

        void comButtonClick ()
        {
            bool markerEnabled = !CoM.activeInHierarchy;
            if (pluginEnabled) {
                bool visible = !CoM.GetComponent<MarkerVisibility> ().CoMToggle;
                CoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                DCoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                ACoM.GetComponent<MarkerVisibility> ().CoMToggle = visible;
                /* we need the CoM to remain active, but we can't stop the editor from
                 * deactivating it when the CoM toggle button is used, so we toggle it now so is
                 * toggled again by the editor. That way it will remain active. */
                CoM.SetActive(markerEnabled);
            }

            if (!pluginEnabled && markerEnabled) {
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
            if (!disableShortcuts && Input.GetKeyDown (Settings.shortcut_key)) {
                Enabled = !Enabled;
            }

            if (ReferenceTransform == null) {
                return;
            }

            if (Enabled) {
                updateModules ();

                /* Switching direction */
                if (!disableShortcuts && Input.anyKeyDown) {
                    if (GameSettings.TRANSLATE_UP.GetKeyDown ()) {
                        switchDirection (Direction.up);
                    } else if (GameSettings.TRANSLATE_DOWN.GetKeyDown ()) {
                        switchDirection (Direction.down);
                    } else if (GameSettings.TRANSLATE_FWD.GetKeyDown ()) {
                        switchDirection (Direction.forward);
                    } else if (GameSettings.TRANSLATE_BACK.GetKeyDown ()) {
                        switchDirection (Direction.back);
                    } else if (GameSettings.TRANSLATE_LEFT.GetKeyDown ()) {
                        switchDirection (Direction.left);
                    } else if (GameSettings.TRANSLATE_RIGHT.GetKeyDown ()) {
                        switchDirection (Direction.right);
                    }
                }
            }
        }

        void onShipModified (ShipConstruct construct)
        {
            /* fired whenever the ship changes, be it de/attach parts, gizmos o tweakables. It
             * doesn't fire when you drag a part in the vessel however */
            Part part = EditorLogic.RootPart;
            if (part != null) {
                ReferenceTransform = part.GetReferenceTransform ();
            }
        }

        void updateModules ()
        {
            switch(Mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
                rcsList = EditorUtils.GetModulesOf<ModuleRCS> ();
                /* Add RCSForce component */
                foreach (PartModule mod in rcsList) {
                    addForce<RCSForce>(mod);
                }
                break;
            case PluginMode.Engine:
                if (Settings.eng_include_rcs) {
                    rcsList = EditorUtils.GetModulesOf<ModuleRCS> ();
                    foreach (PartModule mod in rcsList) {
                        addForce<RCSForce>(mod);
                    }
                }
                engineList = EditorUtils.GetModulesOf<ModuleEngines> ();
                foreach (PartModule mod in engineList) {
                    addForce<EngineForce>(mod);
                }

                List<PartModule> multiModeList = EditorUtils.GetModulesOf<MultiModeEngine> ();
                foreach (PartModule mod in multiModeList) {
                    addForce<MultiModeEngineForce>(mod);
                }

                /* find ModuleEnginesFX parts that aren't using MultiModeEngine */
                var engineFXList = new List<PartModule> ();
                List<PartModule> tempEngList = EditorUtils.GetModulesOf<ModuleEnginesFX>();
                foreach (PartModule mod in tempEngList) {
                    bool found = false;
                    foreach (PartModule mod2 in multiModeList) {
                        if (mod2.part == mod.part) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        addForce<EnginesFXForce>(mod);
                        engineFXList.Add (mod);
                    }
                }
                engineList.AddRange(multiModeList);
                engineList.AddRange(engineFXList);

                /* find the bottommost stage with engines */
                int stage = 0;
                foreach (PartModule mod in engineList) {
                    if (mod.part.inverseStage > stage) {
                        stage = mod.part.inverseStage;
                    }
                }
                LastStage = stage;
                break;
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
            Direction direction = events.direction;
            if (Mode != PluginMode.RCS && Mode != PluginMode.Attitude && Mode != PluginMode.Engine) {
                /* directions only make sense in some modes, so lets enable the last one used. */
                events.SetPreviousMode();
                if (direction == dir) {
                    /* don't disable in this case */
                    return;
                }
            }
            if (direction == dir) {
                /* disabling due to pressing twice the same key */
                events.SetDirection(Direction.none);
                if (Mode != PluginMode.Engine) {
                    events.SetMode (PluginMode.none);
                }
            } else {
                /* enabling RCS vectors or switching direction */
                if (Mode == PluginMode.none) {
                    events.SetPreviousMode();
                }
                events.SetDirection(dir);
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
