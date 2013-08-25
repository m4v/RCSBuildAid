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
using UnityEngine;

namespace RCSBuildAid
{
	public enum Directions { none, right, up, fwd, left, down, back };

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class RCSBuildAid : MonoBehaviour
	{
        public static GameObject DCoM;
        public static GameObject CoM;
        public static GameObject Reference;
		public static bool Rotation = false;
		public static Directions Direction = Directions.none;
        public static List<ModuleRCS> RCSlist;

		int moduleRCSClassID = "ModuleRCS".GetHashCode ();
        int CoMCycle = 0;

		public static Dictionary<Directions, Vector3> Normals
				= new Dictionary<Directions, Vector3>() {
			{ Directions.none,  Vector3.zero    },
			{ Directions.right, Vector3.right   },
			{ Directions.up,    Vector3.up      },
			{ Directions.fwd,	Vector3.forward },
			{ Directions.left,  Vector3.right   * -1 },
			{ Directions.down, 	Vector3.up      * -1 },
			{ Directions.back,  Vector3.forward * -1 }
		};

		void Start () {
			Direction = Directions.none;
			Rotation = false;
			CoM = null;
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
                    Reference = CoM;
                    DCoM = (GameObject)UnityEngine.Object.Instantiate(CoM);
                    DCoM.name = "DCoM Marker";
                    if (DCoM.transform.GetChildCount() > 0) {
                        /* Stock CoM doesn't have any attached objects, if there's some it means
                         * there's a plugin doing the same thing as us. We don't want extra
                         * objects */
                        for (int i = 0; i < DCoM.transform.GetChildCount(); i++) {
                            Destroy(DCoM.transform.GetChild(i).gameObject);
                        }
                    }
                    DCoM.transform.localScale = Vector3.one * 0.9f;
                    DCoM.renderer.material.color = Color.red;
                    DCoM.transform.parent = CoM.transform;
                    Destroy(DCoM.GetComponent<EditorMarker_CoM>()); /* we don't need this */
                    DCoM.AddComponent<DryCoM_Marker>();             /* we do need this    */

                    CoM.AddComponent<CoMVectors>();
                    CoMVectors comv = DCoM.AddComponent<CoMVectors>();
                    comv.enabled = false;
            	}
			}

            if (CoM.activeInHierarchy) {
                List<ModuleRCS> activeRCS = new List<ModuleRCS> ();

                /* find RCS connected to vessel */
                if (EditorLogic.startPod != null) {
                    recursePart (EditorLogic.startPod, activeRCS);
                }

                /* find selected RCS when they are about to be connected */
                if (EditorLogic.SelectedPart != null) {
                    Part part = EditorLogic.SelectedPart;
                    if (part.potentialParent != null) {
                        recursePart (part, activeRCS);
                        foreach (Part p in part.symmetryCounterparts) {
                            recursePart (p, activeRCS);
                        }
                    }
                }
                RCSlist = activeRCS;

                /* Add RCSForce component */
                if (Direction != Directions.none) {
                    foreach (ModuleRCS mod in RCSlist) {
                        RCSForce force = mod.GetComponent<RCSForce> ();
                        if (force == null) {
                            mod.gameObject.AddComponent<RCSForce> ();
                        }
                    }
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
					} else if (Input.GetKeyDown(KeyCode.M)) {
                        CoMVectors comv = CoM.GetComponent<CoMVectors>();
                        CoMVectors dcomv = DCoM.GetComponent<CoMVectors>();
                        switch(CoMCycle) {
                        case 0:
                            comv.enabled = false;
                            dcomv.enabled = true;
                            Reference = DCoM;
                            CoMCycle++;
                            break;
                        case 1:
                        default:
                            comv.enabled = true;
                            dcomv.enabled = false;
                            Reference = CoM;
                            CoMCycle = 0;
                            break;
                        }
                    }
				}
			} else {
				/* CoM disabled */
				Direction = Directions.none;
        		disableAll ();
			}
#if DEBUG
			if (Input.GetKeyDown (KeyCode.Space)) {
                Func<Type, int> getCount = (type) => GameObject.FindObjectsOfType(type).Length;
				print (String.Format ("ModuleRCS count: {0}", getCount(typeof(ModuleRCS))));
				print (String.Format ("RCSForce count: {0}", getCount(typeof(RCSForce))));
				print (String.Format ("VectorGraphic count: {0}", getCount(typeof(VectorGraphic))));
                print (String.Format ("TorqueGraphic count: {0}", getCount(typeof(TorqueGraphic))));
				print (String.Format ("LineRenderer count: {0}", getCount(typeof(LineRenderer))));
            }
#endif
		}

        void disableAll ()
        {
            RCSForce[] forceList = (RCSForce[])GameObject.FindSceneObjectsOfType (typeof(RCSForce));
			foreach (RCSForce force in forceList) {
				Destroy (force);
			}
		}

		void recursePart (Part part, List<ModuleRCS> list)
        {
            /* check if this part is a RCS */
            foreach (PartModule mod in part.Modules) {
                if (mod.ClassID == moduleRCSClassID) {
                    list.Add ((ModuleRCS)mod);
                    break;
                }
            }

			foreach (Part p in part.children) {
				recursePart (p, list);
			}
		}

		void switchDirection (Directions dir)
		{
			bool rotaPrev = Rotation;
			if (Input.GetKey (KeyCode.LeftShift)
			    || Input.GetKey (KeyCode.RightShift)) {
				Rotation = true;
			} else {
				Rotation = false;
			}
			if (Direction == dir && Rotation == rotaPrev) {
                /* disabling due to pressing twice the same key */
				Direction = Directions.none; 
                disableAll ();
                CoM.GetComponent<CoMVectors> ().enabled = false;
                DCoM.GetComponent<CoMVectors> ().enabled = false;
			} else {
                /* enabling RCS vectors  or switching direction */
                if (RCSlist.Count == 0) {
                    ScreenMessages.PostScreenMessage(
                        "No RCS thrusters in place.", 3,
                        ScreenMessageStyle.LOWER_CENTER);
                }
                if (Direction == Directions.none) {
                    /* enabling vectors, making sure the correct CoMVector is enabled */
                    CoM.GetComponent<CoMVectors>().enabled = true;
                    DCoM.GetComponent<CoMVectors>().enabled = false;
                }
				Direction = dir;
			}
		}
	}

    /* Component for calculate and show forces in RCS */
	public class RCSForce : MonoBehaviour
	{
		float thrustPower;
		ModuleRCS module;

		public VectorGraphic[] vectors;

		void Awake ()
		{
			module = GetComponent<ModuleRCS> ();
			if (module == null) {
				throw new Exception ("Missing ModuleRCS component.");
			}
			/* symmetry and clonning do this */
			if (vectors != null) {
				for (int i = 0; i < vectors.Length; i++) {
					Destroy (vectors[i].gameObject);
				}
			}
		}

		void Start ()
		{
            /* thrusterTransforms aren't initialized while in Awake, so in Start instead */
            GameObject obj;
		    int n = module.thrusterTransforms.Count;
            vectors = new VectorGraphic[n];
			for (int i = 0; i < n; i++) {
				obj = new GameObject ("RCSVector");
                obj.layer = 1;
				obj.transform.parent = transform;
                obj.transform.position = module.thrusterTransforms[i].position;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
			}
			thrustPower = module.thrusterPower;
		}

		void Update ()
        {
            float force;
            VectorGraphic vector;
            Vector3 thrust;
            Vector3 normal;
            Vector3 rotForce = Vector3.zero;

            if (RCSBuildAid.Reference == null) {
                return;
            }

            if (!RCSBuildAid.RCSlist.Contains (module)) {
                /* we got disconnected */
                Destroy (this);
                return;
            }

            normal = RCSBuildAid.Normals[RCSBuildAid.Direction];
			if (RCSBuildAid.Rotation) {
				rotForce = Vector3.Cross (transform.position - 
                                          RCSBuildAid.Reference.transform.position, normal);
			}

			/* calculate The Force  */
			for (int t = 0; t < module.thrusterTransforms.Count; t++) {
				thrust = module.thrusterTransforms [t].up;
				if (!RCSBuildAid.Rotation) {
					force = Mathf.Max (Vector3.Dot (thrust, normal), 0f);
				} else {
					force = Mathf.Max (Vector3.Dot (thrust, rotForce), 0f);
				}

				force = Mathf.Clamp (force, 0f, 1f) * thrustPower;
				Vector3 vectorThrust = thrust * force;

				/* update VectorGraphic */
				vector = vectors [t];
                vector.value = vectorThrust;
				/* show it if there's force */
				if (force > 0f) {
					vector.enabled = true;
				} else {
					vector.enabled = false;
				}
			}
		}

		void OnDestroy () 
        {
            for (int i = 0; i < vectors.Length; i++) {
				Destroy (vectors[i].gameObject);
			}
		}
	}


    /* Component for calculate and show forces in CoM */
    public class CoMVectors : MonoBehaviour
    {
        VectorGraphic transVector;
        TorqueGraphic torqueCircle;
        float threshold = 0.05f;

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                transVector.gameObject.SetActive(value);
                torqueCircle.gameObject.SetActive(value);
            }
        }

        void Awake ()
        {
            /* layer change must be done before adding the Graphic components */
            GameObject obj = new GameObject("Translation Vector Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            transVector = obj.AddComponent<VectorGraphic>();
            transVector.width = 0.15f;
            transVector.color = Color.green;
            transVector.offset = 0.6f;
            transVector.maxLength = 3f;

            obj = new GameObject("Torque Circle Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            torqueCircle = obj.AddComponent<TorqueGraphic>();
        }

        void LateUpdate ()
        {
            /* calculate torque, translation and display them */
            Vector3 torque = Vector3.zero;
            Vector3 translation = Vector3.zero;

            /* RCS */
            foreach (PartModule mod in RCSBuildAid.RCSlist) {
                RCSForce RCSf = mod.GetComponent<RCSForce> ();
                if (RCSf == null || RCSf.vectors == null) {
                    /* setup not done yet it seems */
                    continue;
                }
                for (int t = 0; t < RCSf.vectors.Length; t++) {
                    Vector3 distance = RCSf.vectors [t].transform.position -
                        transform.position;
                    Vector3 thrustForce = RCSf.vectors [t].value;
                    Vector3 partialtorque = Vector3.Cross (distance, thrustForce);
                    torque += partialtorque;
                    translation -= thrustForce;
                }
            }

            if (torque != Vector3.zero) {
                torqueCircle.transform.rotation = Quaternion.LookRotation (torque, translation);
            }

            /* update vectors in CoM */
            torqueCircle.value = torque;
            transVector.value = translation;
            if (RCSBuildAid.Rotation) {
                /* rotation mode, we want to reduce translation */
                torqueCircle.enabled = true;
                torqueCircle.valueTarget = RCSBuildAid.Normals [RCSBuildAid.Direction] * -1;
                transVector.valueTarget = Vector3.zero;
                if (translation.magnitude < threshold) {
                    transVector.enabled = false;
                } else {
                    transVector.enabled = true;
                }
            } else {
                /* translation mode, we want to reduce torque */
                transVector.enabled = true;
                transVector.valueTarget = RCSBuildAid.Normals [RCSBuildAid.Direction] * -1;
                torqueCircle.valueTarget = Vector3.zero;
                if (torque.magnitude < threshold) {
                    torqueCircle.enabled = false;
                } else {
                    torqueCircle.enabled = true;
                }
            }
        }
    }

    public class DryCoM_Marker : MonoBehaviour
    {
        Vector3 DCoM_position;
        float partMass;

        void LateUpdate ()
        {
            DCoM_position = Vector3.zero;
            partMass = 0f;

            if (EditorLogic.startPod == null) {
                return;
            }

            recursePart (EditorLogic.startPod);
            if (EditorLogic.SelectedPart != null) {
                Part part = EditorLogic.SelectedPart;
                if (part.potentialParent != null) {
                    recursePart (part);
                    foreach (Part p in part.symmetryCounterparts) {
                        recursePart (p);
                    }
                }
            }

            transform.position = DCoM_position / partMass;
        }

        void recursePart (Part part)
        {
            if (part.physicalSignificance == Part.PhysicalSignificance.FULL) {
                DCoM_position += (part.transform.position 
                                 + part.transform.rotation * part.CoMOffset)
                                 * part.mass;
                partMass += part.mass;
            }
           
            foreach (Part p in part.children) {
                recursePart (p);
            }
        }
    }
}
