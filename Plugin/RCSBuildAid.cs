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
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public enum PluginMode { none, RCS, Attitude, Engine };
    public enum MarkerType { CoM, DCoM, ACoM };

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class RCSBuildAid : MonoBehaviour
    {
        public enum Directions { none, right, left, up, down, forward, back };

        /* Fields */

        static MarkerForces vesselForces;
        static bool pluginEnabled = false;
        static Directions direction;
        static Dictionary<MarkerType, GameObject> referenceDict = 
            new Dictionary<MarkerType, GameObject> ();

        public static bool toolbarEnabled = false;
        public static List<PartModule> RCSlist;
        public static List<PartModule> EngineList;
        public static List<PartModule> WheelList;
        public static int lastStage = 0;
        public static RCSBuildAidEvents events;

        public static GameObject CoM;
        public static GameObject DCoM;
        public static GameObject ACoM;

        EditorVesselOverlays vesselOverlays;

        /* Properties */

        public static Vector3 Normal {
            get {
                if (referenceTransform == null) {
                    return Vector3.zero;
                }
                switch (direction) {
                case Directions.forward:
                    return referenceTransform.up * -1;
                case Directions.back:
                    return referenceTransform.up;
                case Directions.right:
                    return referenceTransform.right * -1;
                case Directions.left:
                    return referenceTransform.right;
                case Directions.up:
                    return referenceTransform.forward;
                case Directions.down:
                    return referenceTransform.forward * -1;
                case Directions.none:
                default:
                    return Vector3.zero;
                }
            }
        }

        public static Transform referenceTransform { get; private set; }
        public static MarkerType referenceMarker { get; private set; }
        public static PluginMode mode { get { return events.mode; } }

        public static GameObject ReferenceMarker {
            get { return GetMarker (referenceMarker); }
        }

        public static MarkerForces VesselForces {
            get { return vesselForces; }
        }

        public static Directions Direction {
            get { return direction; }
            set { direction = value; }
        }

        public static bool Enabled {
            get { 
                if (EditorLogic.fetch.editorScreen != EditorLogic.EditorScreen.Parts) {
                    /* the plugin isn't useful unless in the part screen */
                    return false;
                }
                return pluginEnabled; 
            }
            set { 
                pluginEnabled = value;
                CoM.SetActive (value);
                DCoM.SetActive (value);
                ACoM.SetActive (value);
            }
        }

        /* Methods */

        public static void SetReferenceMarker (MarkerType comref)
        {
            referenceMarker = comref;
            vesselForces.Marker = GetMarker(referenceMarker);
        }

        public static GameObject GetMarker (MarkerType comref)
        {
            return referenceDict [comref];
        }

        public static void onModeChange (PluginMode mode)
        {
            switch (mode) {
            case PluginMode.Engine:
                RCSlist.Clear ();
                WheelList.Clear();
                break;
            case PluginMode.Attitude:
                EngineList.Clear ();
                break;
            case PluginMode.RCS:
                EngineList.Clear ();
                WheelList.Clear();
                break;
            case PluginMode.none:
                clearAllLists ();
                break;
            }
        }

        public static void setMarkerVisibility (MarkerType marker, bool value)
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

        public static bool isMarkerVisible (MarkerType marker)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            return markerVis.isVisible;
        }

        void Awake ()
        {
            Settings.LoadConfig ();
            Load ();

            RCSlist = new List<PartModule> ();
            EngineList = new List<PartModule> ();
            WheelList = new List<PartModule> ();

            events = new RCSBuildAidEvents ();
            events.onModeChange += onModeChange;

            gameObject.AddComponent<MainWindow> ();
            gameObject.AddComponent<DeltaV> ();
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType(
                typeof(EditorVesselOverlays));
        }

        void Load ()
        {
            referenceMarker = (MarkerType)Settings.GetValue("com_reference", 0);
            direction = (Directions)Settings.GetValue("direction", 1);
        }

        void Start ()
        {
            setupMarker (); /* must be in Start because CoMmarker is null in Awake */
            events.SetMode(events.mode);

            /* enable markers if plugin starts active */
            Enabled = pluginEnabled;
        }

        public void CoMButtonClick ()
        {
            bool markerEnabled = !CoM.activeInHierarchy;
            if (!toolbarEnabled) {
                pluginEnabled = markerEnabled;
                DCoM.SetActive (markerEnabled);
                ACoM.SetActive (markerEnabled);
            } else {
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
            }

            if (!pluginEnabled && markerEnabled) {
                /* restore CoM visibility, so the regular CoM toggle button works. */
                CoM.GetComponent<MarkerVisibility> ().Show ();
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
            CoM_Marker comMarker = CoM.AddComponent<CoM_Marker> ();
            comMarker.posMarkerObject = vesselOverlays.CoMmarker.posMarkerObject;
            Destroy (vesselOverlays.CoMmarker);
            vesselOverlays.CoMmarker = comMarker;

            /* setup DCoM */
            DCoM_Marker dcomMarker = DCoM.AddComponent<DCoM_Marker> (); /* we do need this    */
            dcomMarker.posMarkerObject = DCoM;

            /* setup ACoM */
            var acomMarker = ACoM.AddComponent<Average_Marker> ();
            acomMarker.posMarkerObject = ACoM;
            acomMarker.CoM1 = comMarker;
            acomMarker.CoM2 = dcomMarker;

            GameObject obj = new GameObject("Vessel Forces Object");
            obj.layer = CoM.layer;
            vesselForces = obj.AddComponent<MarkerForces> ();
            SetReferenceMarker(referenceMarker);

            /* scaling for CoL and CoT markers */
            vesselOverlays.CoLmarker.gameObject.AddComponent<MarkerScaler> ();
            vesselOverlays.CoTmarker.gameObject.AddComponent<MarkerScaler> ();

            /* attach our method to the CoM toggle button */
            vesselOverlays.toggleCoMbtn.AddValueChangedDelegate(delegate { CoMButtonClick(); });
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Save ()
        {
            Settings.SetValue ("com_reference", (int)referenceMarker);
            if (direction != Directions.none) {
                Settings.SetValue ("direction", (int)direction);
            }
        }

        void Update ()
        {
            if (referenceTransform == null) {
                if (EditorLogic.startPod != null) {
                    referenceTransform = EditorLogic.startPod.GetReferenceTransform();
                } else {
                    return;
                }
            }

            if (Enabled) {
                doPlugingUpdate ();

                /* Switching direction */
                if (Input.anyKeyDown) {
                    if (GameSettings.TRANSLATE_UP.GetKeyDown ()) {
                        switchDirection (Directions.up);
                    } else if (GameSettings.TRANSLATE_DOWN.GetKeyDown ()) {
                        switchDirection (Directions.down);
                    } else if (GameSettings.TRANSLATE_FWD.GetKeyDown ()) {
                        switchDirection (Directions.forward);
                    } else if (GameSettings.TRANSLATE_BACK.GetKeyDown ()) {
                        switchDirection (Directions.back);
                    } else if (GameSettings.TRANSLATE_LEFT.GetKeyDown ()) {
                        switchDirection (Directions.left);
                    } else if (GameSettings.TRANSLATE_RIGHT.GetKeyDown ()) {
                        switchDirection (Directions.right);
                    }
                }
            } else {
                clearAllLists ();
            }
        }

        void doPlugingUpdate ()
        {
            switch(mode) {
            case PluginMode.RCS:
                RCSlist = getModulesOf<ModuleRCS> ();

                /* Add RCSForce component */
                foreach (PartModule mod in RCSlist) {
                    addForce<RCSForce>(mod);
                }
                break;
            case PluginMode.Attitude:
                if (Settings.include_rcs) {
                    RCSlist = getModulesOf<ModuleRCS> ();
                } else {
                    RCSlist.Clear();
                }
                if (Settings.include_wheels) {
                    WheelList = getModulesOf<ModuleReactionWheel> ();
                } else {
                    WheelList.Clear ();
                }

                /* Add RCSForce component */
                foreach (PartModule mod in RCSlist) {
                    addForce<RCSForce>(mod);
                }
                break;
            case PluginMode.Engine:
                List<PartModule> engineList = getModulesOf<ModuleEngines> ();
                foreach (PartModule mod in engineList) {
                    addForce<EngineForce>(mod);
                }

                List<PartModule> multiModeList = getModulesOf<MultiModeEngine> ();
                foreach (PartModule mod in multiModeList) {
                    addForce<MultiModeEngineForce>(mod);
                }

                /* find ModuleEnginesFX parts that aren't using MultiModeEngine */
                List<PartModule> engineFXList = new List<PartModule> ();
                List<PartModule> tempList = getModulesOf<ModuleEnginesFX>();
                foreach (PartModule mod in tempList) {
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
                EngineList = engineList;
                EngineList.AddRange(multiModeList);
                EngineList.AddRange(engineFXList);

                /* find the bottommost stage with engines */
                int stage = 0;
                foreach (PartModule mod in EngineList) {
                    if (mod.part.inverseStage > stage) {
                        stage = mod.part.inverseStage;
                    }
                }
                lastStage = stage;
                break;
            }
        }

        void addForce<T> (PartModule module) where T: ModuleForces
        {
            T force = module.GetComponent<T> ();
            if (force == null) {
                module.gameObject.AddComponent<T> ();
            } else if (!force.enabled) {
                force.Enable ();
            }
        }

        static void switchDirection (Directions dir)
        {
            /* directions only make sense in RCS mode */
            if (mode != PluginMode.RCS && mode != PluginMode.Attitude) {
                events.SetPreviousMode();
                if (direction == dir) {
                    /* don't disable in this case */
                    return;
                }
            }
            if (direction == dir) {
                /* disabling due to pressing twice the same key */
                events.SetMode(PluginMode.none);
                direction = Directions.none;
            } else {
                /* enabling RCS vectors or switching direction */
                if (mode == PluginMode.none) {
                    events.SetPreviousMode();
                }
                direction = dir;
                switch(mode) {
                case PluginMode.RCS:
                    if (getModulesOf<ModuleRCS> ().Count == 0) {
                        ScreenMessages.PostScreenMessage(
                            "No RCS thrusters in place.", 3,
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                case PluginMode.Attitude:
                    if (getModulesOf<ModuleRCS> ().Count == 0 && 
                            getModulesOf<ModuleReactionWheel> ().Count == 0) {
                        ScreenMessages.PostScreenMessage(
                            "No attitude control elements in place.", 3,
                            ScreenMessageStyle.LOWER_CENTER);
                    }
                    break;
                }
            }
        }

        static void clearAllLists ()
        {
            EngineList.Clear ();
            RCSlist.Clear ();
            WheelList.Clear ();
        }

        static void recursePart<T> (Part part, List<PartModule> list) where T : PartModule
        {
            /* check if this part has a module of type T */
            foreach (PartModule mod in part.Modules) {
                if (mod is T) {
                    list.Add (mod);
                    break;
                }
            }

            foreach (Part p in part.children) {
                recursePart<T> (p, list);
            }
        }

        public static List<PartModule> getModulesOf<T> () where T : PartModule
        {
            List<PartModule> list = new List<PartModule> ();

            /* find modules connected to vessel */
            if (EditorLogic.startPod != null) {
                recursePart<T> (EditorLogic.startPod, list);
            }

            /* find selected module when they are about to be connected */
            if (EditorLogic.SelectedPart != null) {
                Part part = EditorLogic.SelectedPart;
                if (part.potentialParent != null) {
                    recursePart<T> (part, list);
                    foreach (Part p in part.symmetryCounterparts) {
                        recursePart<T> (p, list);
                    }
                }
            }
            return list;
        }
	}
}
