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

#if DEBUG
using System.Diagnostics;
#endif

namespace RCSBuildAid
{
	public enum Directions { none, right, up, fwd, left, down, back };

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class RCSBuildAid : MonoBehaviour
	{

		EditorMarker_CoM DCoMmarker;
		VectorGraphic vectorTorque, vectorMovement, vectorCoM;
		GameObject[] ObjVectors = new GameObject[3];

		public static bool Rotation = false;
		public static EditorMarker_CoM CoM;
		public static Directions Direction = Directions.none;

		int moduleRCSClassID = "ModuleRCS".GetHashCode ();
        Vector3 dryCoM;
        float fuelMass;

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

		/* different normals for rotation mode so the input keys
		 * match the rotation direction. */
		public static Dictionary<Directions, Vector3> NormalsRot
				= new Dictionary<Directions, Vector3>() {
			{ Directions.none,  Vector3.zero    },
			{ Directions.right, Vector3.forward },
			{ Directions.up,    Vector3.up      },
			{ Directions.fwd,	Vector3.right   * -1 },
			{ Directions.left,  Vector3.forward * -1 },
			{ Directions.down, 	Vector3.up      * -1 },
			{ Directions.back,  Vector3.right   }
		};

#if DEBUG
		long _counter = 0;
		float _timer, _timer_max, _timer_min;
		Stopwatch _SW = new Stopwatch();
#endif
		void Awake ()
		{
			ObjVectors[0] = new GameObject("TorqueVector");
			ObjVectors[1] = new GameObject("MovementVector");
            ObjVectors[2] = new GameObject("DryCoMVector");
			vectorTorque   = ObjVectors[0].AddComponent<VectorGraphic>();
			vectorMovement = ObjVectors[1].AddComponent<VectorGraphic>();
            vectorCoM = ObjVectors[2].AddComponent<VectorGraphic>();
		}

		void Start () {
			Direction = Directions.none;
			Rotation = false;
			CoM = null;
			vectorTorque.width = 0.08f;
			vectorTorque.color = Color.red;
			vectorMovement.width = 0.15f;
			vectorMovement.color = Color.green;
            vectorCoM.color = Color.yellow;
            vectorCoM.width = 0.15f;
		}

		void LateUpdate ()
		{
#if DEBUG
			_SW.Start ();
#endif
			/* find CoM marker, we need it so we don't have to calculate the CoM ourselves */
			if (CoM == null) {
				CoM = (EditorMarker_CoM)GameObject.FindObjectOfType (typeof(EditorMarker_CoM));
				if (CoM == null) {
					/* nothing to do */
					return;
				} else {
					/* attach our vector GameObjects to CoM */
					foreach (GameObject obj in ObjVectors) {
						obj.transform.parent = CoM.transform;
						obj.transform.localPosition = Vector3.zero;
					}
                    DCoMmarker = ((GameObject)UnityEngine.Object.Instantiate(CoM.gameObject))
                        .GetComponent<EditorMarker_CoM>();
                    DCoMmarker.transform.localScale = Vector3.one * 0.9f;
                    DCoMmarker.transform.parent = CoM.posMarkerObject.transform;
                    DCoMmarker.transform.localPosition = Vector3.zero;
                    DCoMmarker.renderer.material.color = Color.red;
				}
			}

			if (CoM.gameObject.activeInHierarchy) {
                dryCoM = Vector3.zero;
                fuelMass = 0f;

                List<ModuleRCS> activeRCS = new List<ModuleRCS> ();

                /* RCS connected to vessel */
                if (EditorLogic.startPod != null) {
                    recursePart (EditorLogic.startPod, activeRCS);
                }

                /* selected RCS when they are about to be connected */
                if (EditorLogic.SelectedPart != null) {
                    Part part = EditorLogic.SelectedPart;
                    if (part.potentialParent != null) {
                        recursePart (part, activeRCS);
                        foreach (Part p in part.symmetryCounterparts) {
                            recursePart (p, activeRCS);
                        }
                    }
                }
                
                vectorCoM.value = CoM.transform.position - (dryCoM / fuelMass);
                vectorCoM.enabled = true;
                DCoMmarker.transform.localPosition = CoM.transform.position - (dryCoM / fuelMass);

				if (Direction != Directions.none) {
					/* find all RCS */
					ModuleRCS[] RCSList = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));

					/* Show RCS forces */
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

					/* calculate torque, translation and display them */
					Vector3 torque = Vector3.zero;
					Vector3 translation = Vector3.zero;
					foreach (ModuleRCS mod in activeRCS) {
						RCSForce RCSf = mod.GetComponent<RCSForce>();
						if (RCSf.vectors == null) {
							/* didn't Start yet it seems */
							continue;
						}
						for (int t = 0; t < RCSf.vectors.Length; t++) {
							Vector3 distance = RCSf.vectors [t].transform.position - CoM.transform.position;
							Vector3 thrustForce = RCSf.vectorThrust [t];
							Vector3 partialtorque = Vector3.Cross (distance, thrustForce);
							torque += partialtorque;
							translation -= thrustForce;
						}
					}

					/* update vectors in CoM */
					vectorTorque.value = torque;
					vectorMovement.value = translation;
					if (Rotation) {
						/* rotation mode, we want to reduce translation */
						vectorTorque.enabled = true;
						vectorTorque.valueTarget = NormalsRot [Direction] * -1;
						vectorMovement.valueTarget = Vector3.zero;
						if (translation.magnitude < 0.5f) {
							vectorMovement.enabled = false;
						} else {
							vectorMovement.enabled = true;
						}
						/* scale CoM when vector is too small */
						CoM.transform.localScale = Vector3.one *
							Mathf.Clamp (vectorMovement.value.magnitude, 0f, 1f);
					} else {
						/* translation mode, we want to reduce torque */
						vectorMovement.enabled = true;
						vectorMovement.valueTarget = Normals [Direction] * -1;
						vectorTorque.valueTarget = Vector3.zero;
						if (torque.magnitude < 0.5f) {
							vectorTorque.enabled = false;
						} else {
							vectorTorque.enabled = true;
						}
						/* scale CoM when vector is too small */
						CoM.transform.localScale = Vector3.one *
							Mathf.Clamp (vectorTorque.value.magnitude, 0f, 1f);
					}
				} else {
					/* Direction is none */
					disableAll ();
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
					}
				}
			} else {
				/* CoM disabled */
				Direction = Directions.none;
                vectorCoM.enabled = false;
				disableAll ();
			}
#if DEBUG
			_SW.Stop ();
			_counter++;
			if (_counter > 200) {
				_timer = (float)_SW.ElapsedMilliseconds/_counter;
                _timer_max = Mathf.Max(_timer, _timer_max);
                _timer_min = Mathf.Min(_timer, _timer_min);
				_SW.Reset();
				_counter = 0;
			}
			if (Input.GetKeyDown (KeyCode.Space)) {
                print (String.Format("UPDATE time: {0}ms max: {1}ms min: {2}ms", _timer, _timer_max, _timer_min));
				ModuleRCS[] mods = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));
				RCSForce[] forces = (RCSForce[])GameObject.FindObjectsOfType (typeof(RCSForce));
				VectorGraphic[] vectors = (VectorGraphic[])GameObject.FindObjectsOfType (typeof(VectorGraphic));
				LineRenderer[] lines = (LineRenderer[])GameObject.FindObjectsOfType (typeof(LineRenderer));
				print (String.Format ("ModuleRCS count: {0}", mods.Length));
				print (String.Format ("RCSForce count: {0}", forces.Length));
				print (String.Format ("VectorGraphic count: {0}", vectors.Length));
				print (String.Format ("LineRenderer count: {0}", lines.Length));
            }
#endif
		}

		void disableAll ()
		{
			CoM.transform.localScale = Vector3.one;
			vectorTorque.enabled = false;
			vectorMovement.enabled = false;
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
            /* get fuel CoM offset */
            float m = part.GetResourceMass ();
            dryCoM += (part.transform.position + part.transform.rotation * part.CoMOffset) * m;
            fuelMass += m;
           
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
				vectorTorque.width = 0.15f;
				vectorMovement.width = 0.08f;
			} else {
				Rotation = false;
				vectorTorque.width = 0.08f;
				vectorMovement.width = 0.15f;
			}
			if (Direction == dir && Rotation == rotaPrev) {
				Direction = Directions.none;
			} else {
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

			if (RCSBuildAid.Rotation) {
				normal = RCSBuildAid.NormalsRot[RCSBuildAid.Direction];
				rotForce = Vector3.Cross (transform.position - RCSBuildAid.CoM.transform.position, normal);
			} else {
				normal = RCSBuildAid.Normals [RCSBuildAid.Direction];
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

	public class VectorGraphic : MonoBehaviour
	{
		public Vector3 value = Vector3.zero;
		public Vector3 valueTarget = Vector3.zero;
		public float scale = 1;
		public float maxLength = 4;
		public new bool enabled = false;
		string shader = "GUI/Text Shader";

		Color _color = Color.cyan;
		float _width = 0.03f;

		GameObject arrowObj;
		GameObject targetObj;
		LineRenderer line;
		LineRenderer arrow;
		LineRenderer target;

		public Color color {
			get { return _color; }
			set {
				_color = value;
				if (line == null)
					throw new Exception ("line is null");
				if (arrow == null)
					throw new Exception ("arrow is null");
				line.SetColors (_color, _color);
				arrow.SetColors (_color, _color);
				if (target != null) {
					target.SetColors (_color, _color);
				}
			}
		}

		public float width {
			get { return _width; }
			set {
				_width = value;
				if (line == null)
					throw new Exception ("line is null");
				if (arrow == null)
					throw new Exception ("arrow is null");
				line.SetWidth (_width, _width);
				arrow.SetWidth (_width * 3, 0);
				if (target != null) {
					target.SetWidth (0, width);
				}
			}
		}

		void Awake ()
		{
			/* try GetComponent fist, symmetry/clonning adds LineRenderer beforehand. */
			line = GetComponent<LineRenderer> ();
			if (line == null) {
				line = gameObject.AddComponent<LineRenderer> ();
			}
			line.material = new Material (Shader.Find (shader));

			/* arrow point */
			/* NOTE: when clonning the arrow is copied too and the
			 * following causes to get a floating arrow around.
			 * This doesn't happen now because VectorGraphics are
			 * destroyed in RCSForce during clonning/symmetry. */
			arrowObj = new GameObject ("GraphicVectorArrow");
            arrowObj.transform.parent = transform;
            arrowObj.transform.localPosition = Vector3.zero;
            arrow = arrowObj.AddComponent<LineRenderer> ();
			arrow.material = line.material;
		}

		void Start ()
		{
			line.SetVertexCount (2);
			line.SetColors(color, color);
			line.SetWidth (width, width);
			line.enabled = false;

			arrow.SetVertexCount(2);
			arrow.SetColors(color, color);
			arrow.SetWidth(width * 3, 0);
			arrow.enabled = false;
		}

		void LateUpdate ()
		{
			Vector3 v = value;
			if (maxLength > 0 && value.magnitude > maxLength) {
				v = value * (maxLength / value.magnitude);
			}

			Vector3 pStart = transform.position;
			Vector3 pEnd = pStart + (v * scale);
			Vector3 dir = pEnd - pStart;

			/* calculate arrow tip lenght */
			float arrowL = Mathf.Clamp (dir.magnitude / 2f, 0f, width * 4);
			Vector3 pMid = pEnd - dir.normalized * arrowL;

			line.SetPosition (0, pStart);
			line.SetPosition (1, pMid);
			line.enabled = enabled;

			arrow.SetPosition (0, pMid);
			arrow.SetPosition (1, pEnd);
			arrow.enabled = enabled;

			/* target marker */
			if ((valueTarget != Vector3.zero) && enabled) {
				if (target == null) {
					setupTargetMarker();
				}
				Vector3 p1 = pStart + (valueTarget.normalized * (float)v.magnitude);
				Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
				target.SetPosition (0, p1);
				target.SetPosition (1, p2);
				target.enabled = true;
			} else if (target != null) {
				target.enabled = false;
			}
		}

		void setupTargetMarker ()
		{
			targetObj = new GameObject ("GraphicVectorTarget");
			targetObj.transform.parent = transform;
            targetObj.transform.localPosition = Vector3.zero;
            target = targetObj.AddComponent<LineRenderer> ();
			target.material = line.material;

			target.SetVertexCount(2);
			target.SetColors(color, color);
			target.SetWidth (0, width);
			target.enabled = false;
		}
	}
}
