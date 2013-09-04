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
	public enum Directions { none, right, up, fwd, left, down, back };
    public enum RCSMode { TRANSLATION, ROTATION };
    public enum DisplayMode { none, RCS, Engine };
    public enum CoMReference { CoM, DCoM };

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class RCSBuildAid : MonoBehaviour
	{
        public static GameObject DCoM;
        public static GameObject CoM;
        public static RCSMode rcsMode;
        public static DisplayMode mode;
        public static Directions Direction;
        public static List<PartModule> RCSlist;
        public static List<PartModule> EngineList;
        public static int lastStage = 0;

		public static Dictionary<Directions, Vector3> Normals
				= new Dictionary<Directions, Vector3>() {
            { Directions.none,  Vector3.zero         },
            { Directions.right, Vector3.right   * -1 },
            { Directions.up,    Vector3.up           },
            { Directions.fwd,   Vector3.forward * -1 },
            { Directions.left,  Vector3.right        },
            { Directions.down,  Vector3.up      * -1 },
            { Directions.back,  Vector3.forward      }
		};

        static Dictionary<CoMReference, GameObject> referenceDict = 
            new Dictionary<CoMReference, GameObject> ();

        public static CoMReference reference { get; private set; }

        public static GameObject Reference {
            get { return referenceDict [reference]; }
        }

        public static void SetReference (CoMReference comref) 
        {
            reference = comref;
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

        void Awake ()
        {
            gameObject.AddComponent<Window> ();
            rcsMode = RCSMode.TRANSLATION;
            reference = CoMReference.CoM;
            Direction = Directions.right;
        }

		void Start () {
			CoM = null;
            RCSlist = new List<PartModule> ();
            EngineList = new List<PartModule> ();
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
                    disableEngines ();
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
                    disableRCS ();
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

                default:
                    disableRCS ();
                    disableEngines ();
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

        void disableRCS ()
        {
            disableType<RCSForce> (RCSlist);
        }

        void disableEngines ()
        {
            disableType<EngineForce> (EngineList);
        }

        void disableType<T> (List<PartModule> moduleList) where T : ModuleForces
        {
            if (moduleList.Count == 0) {
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

        void recursePart<T> (Part part, List<PartModule> list) where T : PartModule
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

        List<PartModule> getModulesOf<T> () where T : PartModule
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

		void switchDirection (Directions dir)
		{
			if (Direction == dir) {
                /* disabling due to pressing twice the same key */
                mode = DisplayMode.none;
                Direction = Directions.none;
			} else {
                /* enabling RCS vectors or switching direction */
                if (mode == DisplayMode.none) {
                    mode = DisplayMode.RCS;
                }
                Direction = dir;
			}
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
