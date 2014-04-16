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
    public enum PluginMode { none, RCS, Engine, Attitude };
    public enum CoMReference { CoM, DCoM, ACoM };

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class RCSBuildAid : MonoBehaviour
    {
        public enum Directions { none, right, left, up, down, forward, back };

        /* Fields */

        static bool pluginEnabled = false;
        static PluginMode lastMode = PluginMode.RCS;
        static Directions direction;
        static Dictionary<CoMReference, GameObject> referenceDict = 
            new Dictionary<CoMReference, GameObject> ();

        public static bool toolbarEnabled = false;
        public static List<PartModule> RCSlist;
        public static List<PartModule> EngineList;
        public static List<PartModule> WheelList;
        public static int lastStage = 0;

        public static GameObject CoM;
        public static GameObject DCoM;
        public static GameObject ACoM;

        static MarkerForces vesselForces;
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
        public static CoMReference referenceMarker { get; private set; }
        public static PluginMode mode { get; private set; }

        public static GameObject ReferenceMarker {
            get { return referenceDict [referenceMarker]; }
        }

        public static MarkerForces VesselForces {
            get { return vesselForces; }
        }

        public static Directions Direction {
            get { return direction; }
            set { direction = value; }
        }

        public static void SetReferenceMarker (CoMReference comref)
        {
            referenceMarker = comref;
            if (CoM == null) {
                return;
            }
            switch(referenceMarker) {
            case CoMReference.DCoM:
                vesselForces.Marker = DCoM;
                break;
            case CoMReference.CoM:
                vesselForces.Marker = CoM;
                break;
            case CoMReference.ACoM:
                vesselForces.Marker = ACoM;
                break;
            }
        }

        public static GameObject GetMarker (CoMReference comref)
        {
            return referenceDict [comref];
        }

        public static void SetMode (PluginMode mode)
        {
            switch(RCSBuildAid.mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
                /* need to remember this for returning to this mode when using shortcuts */
                lastMode = RCSBuildAid.mode;
                break;
            }

            RCSBuildAid.mode = mode;
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

        public static bool Enabled {
            get { return pluginEnabled; }
            set { 
                if (!toolbarEnabled) {
                    return;
                }
                pluginEnabled = value;
                CoM.SetActive (value);
                DCoM.SetActive (value);
                ACoM.SetActive (value);
            }
        }

        /* Methods */

        public static void setMarkerVisibility (CoMReference marker, bool value)
        {
            GameObject markerObj = referenceDict [marker];
            if (value) {
                if (!markerObj.activeInHierarchy) {
                    markerObj.SetActive (value);
                }
                markerObj.renderer.enabled = value;
            } else {
                if (markerObj.activeInHierarchy) {
                    markerObj.renderer.enabled = value;
                }
            }
        }

        public static bool isMarkerVisible (CoMReference marker)
        {
            GameObject markerObj = referenceDict [marker];
            return markerObj.activeInHierarchy && markerObj.renderer.enabled;
        }

        void Awake ()
        {
            Settings.LoadConfig ();
            Load ();

            RCSlist = new List<PartModule> ();
            EngineList = new List<PartModule> ();
            WheelList = new List<PartModule> ();

            gameObject.AddComponent<Window> ();
            gameObject.AddComponent<DeltaV> ();
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType(
                typeof(EditorVesselOverlays));
        }

        void Load ()
        {
            referenceMarker = (CoMReference)Settings.GetValue("com_reference", 0);
            direction = (Directions)Settings.GetValue("direction", 1);
        }

        void Start ()
        {
            setupMarker (); /* must be in Start because CoMmarker is null in Awake */
            if (pluginEnabled) {
                /* if the plugin starts active, so should be CoM */
                CoM.SetActive (true);
            }
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
                    DCoM.SetActive (markerEnabled);
                    ACoM.SetActive (markerEnabled);
                }
            }

            if (!pluginEnabled && markerEnabled) {
                /* restore CoM visibility, so the regular CoM toggle button works. */
                CoM.renderer.enabled = true;
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

            referenceDict[CoMReference.CoM] = CoM;
            referenceDict[CoMReference.DCoM] = DCoM;
            referenceDict[CoMReference.ACoM] = ACoM;

            ACoM.renderer.enabled = false;

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

            if (pluginEnabled) {
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
            debugPrint (); /* definition in Debug.cs */
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
                SetMode(lastMode);
                if (direction == dir) {
                    /* don't disable in this case */
                    return;
                }
            }
            if (direction == dir) {
                /* disabling due to pressing twice the same key */
                SetMode(PluginMode.none);
                direction = Directions.none;
            } else {
                /* enabling RCS vectors or switching direction */
                if (mode == PluginMode.none) {
                    SetMode(lastMode);
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
