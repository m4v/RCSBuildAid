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
		public static bool Rotation = false;
		public static Directions Direction = Directions.none;

		int moduleRCSClassID = "ModuleRCS".GetHashCode ();

		/* Key bindings, seems to be backwards, but is the resulf of
		 * RCS forces actually being displayed backwards. */
		Dictionary<Directions, KeyCode> KeyBinding
				= new Dictionary<Directions, KeyCode>() {
			{ Directions.up,    KeyCode.N },
			{ Directions.down,  KeyCode.H },
			{ Directions.left,  KeyCode.L },
			{ Directions.right, KeyCode.J },
			{ Directions.fwd,   KeyCode.K },
			{ Directions.back,  KeyCode.I }
		};

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

		void Awake ()
		{
		}

		void Start () {
			Direction = Directions.none;
			Rotation = false;
			CoM = null;
		}

		void Update ()
		{
			/* find CoM marker, we need it so we don't have to calculate the CoM ourselves */
			if (CoM == null) {
				EditorMarker_CoM _CoM = 
                    (EditorMarker_CoM)GameObject.FindObjectOfType (typeof(EditorMarker_CoM));
				if (_CoM == null) {
					/* nothing to do */
					return;
				} else {
                    /* Setup CoM and DCoM */
                    CoM = _CoM.gameObject;
                    DCoM = (GameObject)UnityEngine.Object.Instantiate(CoM);
                    DCoM.transform.localScale = Vector3.one * 0.9f;
                    DCoM.renderer.material.color = Color.red;
                    Destroy(DCoM.GetComponent<EditorMarker_CoM>()); // we don't need this
                    DCoM.AddComponent<DryCoM_Marker>();             // we do need this

                    CoM.AddComponent<CoMVectors>();
                    CoMVectors comv = DCoM.AddComponent<CoMVectors>();
                    comv.enabled = false;
            	}
			}

            DCoM.SetActive(CoM.activeInHierarchy);

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

                CoMVectors.RCSlist = activeRCS;

				if (Direction != Directions.none) {
					/* find all RCS and add or remove the RCSForce behaviour */
					ModuleRCS[] RCSList = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));

					foreach (ModuleRCS mod in RCSList) {
						RCSForce force = mod.GetComponent<RCSForce> ();
						if (activeRCS.Contains (mod)) {
							if (force == null) {
								force = mod.gameObject.AddComponent<RCSForce> ();
							}
						} else {
							/* Not connected RCS, disable forces */
							if (force != null) {
								Destroy (force);
							}
						}
					}
				} else {
					/* Direction is none */
					disableAll ();
                    CoM.GetComponent<CoMVectors> ().enabled = false;
                    DCoM.GetComponent<CoMVectors> ().enabled = false;
				}

				/* Switching direction */
				if (Input.anyKeyDown) {
					if (Input.GetKeyDown (KeyBinding [Directions.up])) {
						switchDirection (Directions.up);
					} else if (Input.GetKeyDown (KeyBinding [Directions.down])) {
						switchDirection (Directions.down);
					} else if (Input.GetKeyDown (KeyBinding [Directions.fwd])) {
						switchDirection (Directions.fwd);
					} else if (Input.GetKeyDown (KeyBinding [Directions.back])) {
						switchDirection (Directions.back);
					} else if (Input.GetKeyDown (KeyBinding [Directions.left])) {
						switchDirection (Directions.left);
					} else if (Input.GetKeyDown (KeyBinding [Directions.right])) {
						switchDirection (Directions.right);
					} else if (Input.GetKeyDown(KeyCode.M)) {
                        CoMVectors comv = CoM.GetComponent<CoMVectors>();
                        CoMVectors dcomv = DCoM.GetComponent<CoMVectors>();
                        comv.enabled = !comv.enabled;
                        dcomv.enabled = !dcomv.enabled;
                    }
				}
			} else {
				/* CoM disabled */
				Direction = Directions.none;
        		disableAll ();
			}
#if DEBUG
			if (Input.GetKeyDown (KeyCode.Space)) {
				ModuleRCS[] mods = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));
				RCSForce[] forces = (RCSForce[])GameObject.FindObjectsOfType (typeof(RCSForce));
				VectorGraphic[] vectors = (VectorGraphic[])GameObject.FindObjectsOfType (typeof(VectorGraphic));
				LineRenderer[] lines = (LineRenderer[])GameObject.FindObjectsOfType (typeof(LineRenderer));
                TorqueGraphic[] circles = (TorqueGraphic[])GameObject.FindObjectsOfType (typeof(TorqueGraphic));
				print (String.Format ("ModuleRCS count: {0}", mods.Length));
				print (String.Format ("RCSForce count: {0}", forces.Length));
				print (String.Format ("VectorGraphic count: {0}", vectors.Length));
                print (String.Format ("CircleGraphic count: {0}", circles.Length));
				print (String.Format ("LineRenderer count: {0}", lines.Length));
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
				Direction = Directions.none;
			} else {
                if (Direction == Directions.none) {
                    CoM.GetComponent<CoMVectors>().enabled = true;
                }
				Direction = dir;
			}
		}
	}

	public class RCSForce : MonoBehaviour
	{
		float thrustPower;
		ModuleRCS module;

		public GameObject[] vectors;
		public Vector3[] vectorThrust;

		void Awake ()
		{
			module = GetComponent<ModuleRCS> ();
			if (module == null) {
				throw new Exception ("missing ModuleRCS component");
			}
			/* symmetry and clonning do this */
			if (vectors != null) {
				for (int i = 0; i < vectors.Length; i++) {
					Destroy (vectors[i]);
				}
			}
		}

		void Start ()
		{
			int n = module.thrusterTransforms.Count;
			vectors = new GameObject[n];
			vectorThrust = new Vector3[n];
			for (int i = 0; i < n; i++) {
				vectors [i] = new GameObject ("RCSVector");
				vectors [i].AddComponent<VectorGraphic> ();
				vectors [i].transform.parent = transform;
				vectors [i].transform.position = module.thrusterTransforms[i].transform.position;
				vectorThrust [i] = Vector3.zero;
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

            if (RCSBuildAid.CoM == null) {
                return;
            }

            normal = RCSBuildAid.Normals[RCSBuildAid.Direction];
			if (RCSBuildAid.Rotation) {
				rotForce = Vector3.Cross (transform.position - 
                                          RCSBuildAid.CoM.transform.position, normal);
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
				vectorThrust [t] = thrust * force;

				/* update VectorGraphic */
				vector = vectors [t].GetComponent<VectorGraphic> ();
				vector.value = vectorThrust [t];
				/* show it if there's force */
				if (force > 0f) {
					vector.enabled = true;
				} else {
					vector.enabled = false;
				}
			}
		}

		void OnDestroy () {
			foreach (GameObject obj in vectors) {
				Destroy (obj);
			}
		}
	}

    public class CoMVectors : MonoBehaviour
    {
        VectorGraphic transVector;
        TorqueGraphic torqueCircle;
        GameObject transVectorObj = new GameObject("TranslationVector");
        float threshold = 0.05f;

        static Dictionary<Directions, Vector3> Normals = RCSBuildAid.Normals;

        public static List<ModuleRCS> RCSlist;

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                transVectorObj.SetActive(value);
                torqueCircle.gameObject.SetActive(value);
            }
        }

        void Awake ()
        {
            /* for show on top of everything. 
             * must be done before adding the vector components */
            transVectorObj.layer = gameObject.layer;

            transVector = transVectorObj.AddComponent<VectorGraphic>();
            transVectorObj.transform.parent = transform;
            transVectorObj.transform.localPosition = Vector3.zero;
            transVector.width = 0.3f;
            transVector.color = Color.green;
            transVector.offset = 0.6f;
            transVector.maxLength = 3f;

            GameObject obj = new GameObject("TorqueCircle");
            obj.layer = gameObject.layer;

            torqueCircle = obj.AddComponent<TorqueGraphic>();
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
        }

        void LateUpdate ()
        {
            if (!enabled) {
                return;
            }

            /* calculate torque, translation and display them */
            Vector3 torque = Vector3.zero;
            Vector3 translation = Vector3.zero;
            foreach (PartModule mod in RCSlist) {
                RCSForce RCSf = mod.GetComponent<RCSForce>();
                if (RCSf != null && RCSf.vectors == null) {
                    /* didn't Start yet it seems */
                    continue;
                }
                for (int t = 0; t < RCSf.vectors.Length; t++) {
                    Vector3 distance = RCSf.vectors [t].transform.position -
                        transform.position;
                    Vector3 thrustForce = RCSf.vectorThrust [t];
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
                torqueCircle.valueTarget = Normals [RCSBuildAid.Direction] * -1;
                transVector.valueTarget = Vector3.zero;
                transVector.width = 0.08f;
                if (translation.magnitude < threshold) {
                    transVector.enabled = false;
                } else {
                    transVector.enabled = true;
                }
            } else {
                /* translation mode, we want to reduce torque */
                transVector.enabled = true;
                transVector.width = 0.15f;
                transVector.valueTarget = Normals [RCSBuildAid.Direction] * -1;
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
