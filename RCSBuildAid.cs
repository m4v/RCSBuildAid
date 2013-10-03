/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public enum RCSMode { TRANSLATION, ROTATION };
    public enum DisplayMode { none, RCS, Engine };
    public enum CoMReference { CoM, DCoM };

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class RCSBuildAid : MonoBehaviour
	{
        enum Directions { none, right, up, fwd, left, down, back };

        static Directions direction;
		static Dictionary<Directions, Vector3> normals
				= new Dictionary<Directions, Vector3>() {
            { Directions.none,  Vector3.zero         },
            { Directions.right, Vector3.right   * -1 },
            { Directions.up,    Vector3.forward      },
            { Directions.fwd,   Vector3.up      * -1 },
            { Directions.left,  Vector3.right        },
            { Directions.down,  Vector3.forward * -1 },
            { Directions.back,  Vector3.up           }
		};
        static Dictionary<CoMReference, GameObject> referenceDict = 
            new Dictionary<CoMReference, GameObject> ();

        static GameObject DCoM;
        static GameObject CoM;
        public static RCSMode rcsMode;
        public static List<PartModule> RCSlist;
        public static List<PartModule> EngineList;
        public static int lastStage = 0;

        public static Vector3 Normal {
            get { return normals [direction]; }
        }

        public static CoMReference reference { get; private set; }
        public static DisplayMode mode { get; private set; }

        public static GameObject Reference {
            get { return referenceDict [reference]; }
        }

        public static void SetReference (CoMReference comref)
        {
            reference = comref;
            if (CoM == null) {
                return;
            }
            CoMVectors comv = CoM.GetComponent<CoMVectors> ();
            CoMVectors dcomv = DCoM.GetComponent<CoMVectors> ();
            switch(reference) {
            case CoMReference.DCoM:
                comv.enabled = false;
                dcomv.enabled = true;
                break;
            case CoMReference.CoM:
                comv.enabled = true;
                dcomv.enabled = false;
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
                if (CoM == null) {
                    return false;
                } else if (!CoM.activeInHierarchy) {
                    return false;
                }
                return true;
            }
        }

        void Awake ()
        {
            Settings.LoadConfig ();

            gameObject.AddComponent<Window> ();
            direction = Directions.right;
            RCSlist = new List<PartModule> ();
            EngineList = new List<PartModule> ();
            Load ();
        }

        void Load ()
        {
            RCSBuildAid.SetReference((CoMReference)Settings.GetValue("com_reference", 0));
            RCSBuildAid.rcsMode = (RCSMode)Settings.GetValue ("rcs_mode", 0);
        }

        void Save ()
        {
            Settings.SetValue ("com_reference", (int)reference);
            Settings.SetValue ("rcs_mode", (int)rcsMode);
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

		void Update ()
        {
            /* find CoM marker, we need it so we don't have to calculate the CoM ourselves 
             * and as a turn on/off button for our plugin */
            if (CoM == null) {
                /* Is there a better way of finding the CoM object? */
                EditorMarker_CoM _CoM = 
                    (EditorMarker_CoM)GameObject.FindObjectOfType (typeof(EditorMarker_CoM));
                if (_CoM == null) {
                    /* nothing to do */
                    return;
                } else {
                    /* Setup CoM and DCoM */
                    CoM = _CoM.gameObject;
                    DCoM = (GameObject)UnityEngine.Object.Instantiate (CoM);
                    DCoM.name = "DCoM Marker";
                    if (DCoM.transform.GetChildCount () > 0) {
                        /* Stock CoM doesn't have any attached objects, if there's some it means
                         * there's a plugin doing the same thing as us. We don't want extra
                         * objects */
                        for (int i = 0; i < DCoM.transform.GetChildCount(); i++) {
                            Destroy (DCoM.transform.GetChild (i).gameObject);
                        }
                    }
                    DCoM.transform.localScale = Vector3.one * 0.9f;
                    DCoM.renderer.material.color = Color.red;
                    DCoM.transform.parent = CoM.transform;
                    Destroy (DCoM.GetComponent<EditorMarker_CoM> ()); /* we don't need this */
                    DCoM.AddComponent<DryCoM_Marker> ();              /* we do need this    */

                    CoM.AddComponent<CoMVectors> ();
                    DCoM.AddComponent<CoMVectors> ();

                    referenceDict[CoMReference.CoM] = CoM;
                    referenceDict[CoMReference.DCoM] = DCoM;
                    SetReference(reference);
                }
            }

            if (CoM.activeInHierarchy) {
                switch(mode) {
                case DisplayMode.RCS:
                    RCSlist = getModulesOf<ModuleRCS> ();

                    /* Add RCSForce component */
                    foreach (PartModule mod in RCSlist) {
                        RCSForce force = mod.GetComponent<RCSForce> ();
                        if (force == null) {
                            mod.gameObject.AddComponent<RCSForce> ();
                        } else {
                            force.Enable ();
                        }
                    }
                    break;
                case DisplayMode.Engine:
                    EngineList = getModulesOf<ModuleEngines> ();

                    int stage = 0;
                    foreach (PartModule mod in EngineList) {
                        if (mod.part.inverseStage > stage) {
                            stage = mod.part.inverseStage;
                        }
                        EngineForce force = mod.GetComponent<EngineForce> ();
                        if (force == null) {
                            mod.gameObject.AddComponent<EngineForce> ();
                        } else {
                            force.Enable ();
                        }
                    }
                    lastStage = stage;
                    break;
                }

                /* Switching direction */
                if (Input.anyKeyDown) {
                    if (GameSettings.TRANSLATE_UP.GetKeyDown ()) {
                        switchDirection (Directions.up);
                    } else if (GameSettings.TRANSLATE_DOWN.GetKeyDown ()) {
                        switchDirection (Directions.down);
                    } else if (GameSettings.TRANSLATE_FWD.GetKeyDown ()) {
                        switchDirection (Directions.fwd);
                    } else if (GameSettings.TRANSLATE_BACK.GetKeyDown ()) {
                        switchDirection (Directions.back);
                    } else if (GameSettings.TRANSLATE_LEFT.GetKeyDown ()) {
                        switchDirection (Directions.left);
                    } else if (GameSettings.TRANSLATE_RIGHT.GetKeyDown ()) {
                        switchDirection (Directions.right);
                    }
                }
            } else {
                /* CoM disabled */
                disableRCS ();
                disableEngines ();
            }

            debugPrint ();
        }

        static void switchDirection (Directions dir)
        {
            if (direction == dir) {
                /* disabling due to pressing twice the same key */
                mode = DisplayMode.none;
                direction = Directions.none;
            } else {
                /* enabling RCS vectors or switching direction */
                if (mode == DisplayMode.none) {
                    mode = DisplayMode.RCS;
                }
                direction = dir;
            }
        }

        static void disableRCS ()
        {
            disableType<RCSForce> (RCSlist);
        }

        static void disableEngines ()
        {
            disableType<EngineForce> (EngineList);
        }

        static void disableType<T> (List<PartModule> moduleList) where T : ModuleForces
        {
            if ((moduleList == null) || (moduleList.Count == 0)) {
                return;
            }
            for (int i = 0; i < moduleList.Count; i++) {
                PartModule mod = moduleList [i];
                if (mod != null) {
                    ModuleForces mf = mod.GetComponent<T> ();
                    if (mf != null) {
                        mf.Disable ();
                    }
                }
            }
            moduleList.Clear ();
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

        /*
         * Debug stuff
         */

        [Conditional("DEBUG")]
        void debugPrint ()
        {
            if (Input.GetKeyDown (KeyCode.Space)) {
                Func<Type, int> getCount = (type) => GameObject.FindObjectsOfType (type).Length;
                print (String.Format ("ModuleRCS: {0}", getCount (typeof(ModuleRCS))));
                print (String.Format ("ModuleEngines: {0}", getCount (typeof(ModuleEngines))));
                print (String.Format ("RCSForce: {0}", getCount (typeof(RCSForce))));
                print (String.Format ("EngineForce: {0}", getCount (typeof(EngineForce))));
                print (String.Format ("VectorGraphic: {0}", getCount (typeof(VectorGraphic))));
                print (String.Format ("TorqueGraphic: {0}", getCount (typeof(TorqueGraphic))));
                print (String.Format ("LineRenderer: {0}", getCount (typeof(LineRenderer))));
            }
        }
	}
}
