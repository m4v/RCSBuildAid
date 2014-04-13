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
    public enum RCSMode { TRANSLATION, ROTATION };
    public enum DisplayMode { none, RCS, Engine };
    public enum CoMReference { CoM, DCoM, ACoM };

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public partial class RCSBuildAid : MonoBehaviour
    {
        public enum Directions { none, right, left, up, down, forward, back };

        /* Fields */

        static bool pluginEnabled = false;
        static Directions direction;
        static Transform referenceTransform;
        static Dictionary<CoMReference, GameObject> referenceDict = 
            new Dictionary<CoMReference, GameObject> ();
        static Dictionary<CoMReference, MarkerVectors> referenceVectorDict = 
            new Dictionary<CoMReference, MarkerVectors> ();

        public static bool toolbarEnabled = false;
        public static RCSMode rcsMode;
        public static List<PartModule> RCSlist;
        public static List<PartModule> EngineList;
        public static int lastStage = 0;

        public static GameObject CoM;
        public static MarkerVectors CoMV;
        public static GameObject DCoM;
        public static MarkerVectors DCoMV;
        public static GameObject ACoM;
        public static MarkerVectors ACoMV;

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

        public static CoMReference reference { get; private set; }
        public static DisplayMode mode { get; private set; }

        public static GameObject Reference {
            get { return referenceDict [reference]; }
        }

        public static MarkerVectors ReferenceVector {
            get { return referenceVectorDict [reference]; }
        }

        public static Directions Direction {
            get { return direction; }
            set { direction = value; }
        }

        public static void SetReference (CoMReference comref)
        {
            reference = comref;
            if (CoM == null) {
                return;
            }
            switch(reference) {
            case CoMReference.DCoM:
                CoMV.enabled = false;
                ACoMV.enabled = false;
                DCoMV.enabled = true;
                break;
            case CoMReference.CoM:
                if (toolbarEnabled) {
                    CoMV.enabled = true;
                } else {
                    CoMV.enabled = showCoM;
                }
                DCoMV.enabled = false;
                ACoMV.enabled = false;
                break;
            case CoMReference.ACoM:
                CoMV.enabled = false;
                DCoMV.enabled = false;
                ACoMV.enabled = true;
                break;
            }
        }

        public static void SetMode (DisplayMode mode)
        {
            RCSBuildAid.mode = mode;
            switch (mode) {
            case DisplayMode.Engine:
                disableRCS ();
                break;
            case DisplayMode.RCS:
                disableEngines ();
                break;
            case DisplayMode.none:
                disableEngines ();
                disableRCS ();
                break;
            }
        }

        public static bool Enabled {
            get { 
                if (toolbarEnabled) {
                    return pluginEnabled; 
                } else {
                    return CoM.activeInHierarchy;
                }
            }
            set {
                pluginEnabled = value;
                CoM.SetActive (value);
            }
        }

        public static bool showDCoM {
            get { return DCoM.renderer.enabled; }
            set { showMarker (CoMReference.DCoM, value); }
        }

        public static bool showCoM {
            get { return CoM.activeInHierarchy && CoM.renderer.enabled; }
            set { showMarker(CoMReference.CoM, value); }
        }

        public static bool showACoM {
            get { return ACoM.renderer.enabled; }
            set { showMarker(CoMReference.ACoM, value); }
        }

        /* Methods */

        public static void showMarker (CoMReference marker, bool value)
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

            gameObject.AddComponent<Window> ();
            gameObject.AddComponent<DeltaV> ();
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType(
                typeof(EditorVesselOverlays));
        }

        void Load ()
        {
            reference = (CoMReference)Settings.GetValue("com_reference", 0);
            rcsMode = (RCSMode)Settings.GetValue ("rcs_mode", 0);
            direction = (Directions)Settings.GetValue("direction", 1);
        }

        void Start ()
        {
            setupMarker (); /* must be in Start because CoMmarker is null in Awake */
            if (toolbarEnabled && pluginEnabled && !CoM.activeInHierarchy) {
                /* if the plugin starts active, so should be CoM */
                CoM.SetActive (true);
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

            /* Can't attach CoMVector to the CoM markers or they will be affected by their scale */
            GameObject obj = new GameObject("CoM Vector");
            obj.layer = CoM.layer;
            CoMV = obj.AddComponent<MarkerVectors> ();
            CoMV.Marker = CoM;

            obj = new GameObject("DCoM Vector");
            obj.layer = DCoM.layer;
            DCoMV = obj.AddComponent<MarkerVectors> ();
            DCoMV.Marker = DCoM;

            obj = new GameObject("ACoM Vector");
            obj.layer = ACoM.layer;
            ACoMV = obj.AddComponent<MarkerVectors> ();
            ACoMV.Marker = ACoM;

            referenceDict[CoMReference.CoM] = CoM;
            referenceVectorDict[CoMReference.CoM] = CoMV;
            referenceDict[CoMReference.DCoM] = DCoM;
            referenceVectorDict[CoMReference.DCoM] = DCoMV;
            referenceDict[CoMReference.ACoM] = ACoM;
            referenceVectorDict[CoMReference.ACoM] = ACoMV;

            ACoM.renderer.enabled = false;

            /* scaling for CoL and CoT markers */
            vesselOverlays.CoLmarker.gameObject.AddComponent<MarkerScaler> ();
            vesselOverlays.CoTmarker.gameObject.AddComponent<MarkerScaler> ();
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Save ()
        {
            Settings.SetValue ("com_reference", (int)reference);
            Settings.SetValue ("rcs_mode", (int)rcsMode);
            if (direction != Directions.none) {
                Settings.SetValue ("direction", (int)direction);
            }
        }

        void Update ()
        {
            bool enabled = Enabled;
            if (referenceTransform == null) {
                if (EditorLogic.startPod != null) {
                    referenceTransform = EditorLogic.startPod.GetReferenceTransform();
                } else {
                    return;
                }
            }

            if (DCoM.activeInHierarchy != enabled) {
                DCoM.SetActive (enabled);
            }
            if (ACoM.activeInHierarchy != enabled) {
                ACoM.SetActive (enabled);
            }

            if (enabled) {
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
                disableRCS ();
                disableEngines ();

                if (toolbarEnabled && CoM.activeInHierarchy && !showCoM) {
                    /* restore CoM visibility, so the regular CoM toggle button works. */
                    showCoM = true;
                }
            }
            debugPrint (); /* definition in Debug.cs */
        }

        void doPlugingUpdate ()
        {
            switch(mode) {
            case DisplayMode.RCS:
                RCSlist = getModulesOf<ModuleRCS> ();

                /* Add RCSForce component */
                foreach (PartModule mod in RCSlist) {
                    addForce<RCSForce>(mod);
                }
                break;
            case DisplayMode.Engine:
                List<PartModule> engineList = getModulesOf<ModuleEngines> ();
                foreach (PartModule mod in engineList) {
                    addForce<EngineForce>(mod);
                }

                List<PartModule> multiModeList = getModulesOf<MultiModeEngine> ();
                foreach (PartModule mod in multiModeList) {
                    addForce<MultiModeEngineForce>(mod);
                }

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
            if (mode != DisplayMode.RCS) {
                SetMode(DisplayMode.RCS);
                if (direction == dir) {
                    /* don't disable in this case */
                    return;
                }
            }
            if (direction == dir) {
                /* disabling due to pressing twice the same key */
                SetMode(DisplayMode.none);
                direction = Directions.none;
            } else {
                /* enabling RCS vectors or switching direction */
                if (mode == DisplayMode.none) {
                    SetMode(DisplayMode.RCS);
                }
                direction = dir;
                if (getModulesOf<ModuleRCS> ().Count == 0) {
                    ScreenMessages.PostScreenMessage(
                        "No RCS thrusters in place.", 3,
                        ScreenMessageStyle.LOWER_CENTER);
                }
            }
        }

        static void disableRCS ()
        {
            RCSlist.Clear ();
        }

        static void disableEngines ()
        {
            EngineList.Clear ();
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

        static List<PartModule> getModulesOf<T> () where T : PartModule
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
