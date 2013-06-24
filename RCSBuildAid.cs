// Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.


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

		VectorGraphic vectorTorque, vectorMovement, vectorInput;
		Directions direction = Directions.none;
		GameObject[] ObjVectors = new GameObject[3];

		public static bool Rotation = false;
		public static EditorMarker_CoM CoM;

		int moduleRCSClassID = "ModuleRCS".GetHashCode ();

		Dictionary<Directions, KeyCode> KeyBinding = new Dictionary<Directions, KeyCode>() {
			{ Directions.up,    KeyCode.N },
			{ Directions.down,  KeyCode.H },
			{ Directions.left,  KeyCode.L },
			{ Directions.right, KeyCode.J },
			{ Directions.fwd,   KeyCode.K },
			{ Directions.back,  KeyCode.I }
		};

		Dictionary<Directions, Vector3> Normals = new Dictionary<Directions, Vector3>() {
			{ Directions.none,  Vector3.zero },
			{ Directions.right, Vector3.right },
			{ Directions.up,    Vector3.up },
			{ Directions.fwd,	Vector3.forward },
			{ Directions.left,  Vector3.right * -1 },
			{ Directions.down, 	Vector3.up * -1 },
			{ Directions.back,  Vector3.forward * -1 }
		};

#if DEBUG
		long _counter = 0;
		double _timer = 0;
		Stopwatch _SW = new Stopwatch();
#endif
		void Awake ()
		{
			ObjVectors[0] = new GameObject("TorqueVector");
			ObjVectors[1] = new GameObject("MovementVector");
			ObjVectors[2] = new GameObject("InputVector");

			vectorTorque   = ObjVectors[0].AddComponent<VectorGraphic>();
			vectorMovement = ObjVectors[1].AddComponent<VectorGraphic>();
			vectorInput    = ObjVectors[2].AddComponent<VectorGraphic>();
			vectorTorque.width = 0.08f;
			vectorTorque.color = Color.red;
			vectorMovement.width = 0.15f;
			vectorMovement.color = Color.green;
			vectorInput.color = Color.green;
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
				}
			}
			if (CoM.gameObject.activeInHierarchy) {
				if (direction != Directions.none) {
					/* find all RCS */
					ModuleRCS[] RCSList = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));
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

					/* Show RCS forces */
					foreach (ModuleRCS mod in RCSList) {
						RCSForce force = mod.part.transform.GetComponent<RCSForce> ();
						if (activeRCS.Contains (mod)) {
							if (force == null) {
								force = mod.gameObject.AddComponent<RCSForce> ();
							}
							force.direction = direction;
						} else {
							/* Not connected RCS, disable forces */
							if (force != null) {
								Destroy (force);
							}
						}
					}

					/* display translation and torque */
					vectorTorque.enabled = true;
					vectorMovement.enabled = true;
					vectorInput.enabled = true;

					/* size of CoM proportional to vector's magnitude */
					if (!Rotation) {
						/* translation mode, we want to reduce torque */
						CoM.transform.localScale = Vector3.one *
							Mathf.Clamp (vectorTorque.value.magnitude, 0f, 1f);
					} else {
						/* rotation mode, we want to reduce translation */
						CoM.transform.localScale = Vector3.one *
							Mathf.Clamp (vectorMovement.value.magnitude, 0f, 1f);
					}

					/* attach our vector GameObjects to CoM */
					foreach (GameObject obj in ObjVectors) {
						if (obj.transform.parent == null) {
							obj.transform.parent = CoM.transform;
							obj.transform.localPosition = Vector3.zero;
						}
					}
					ShowCoMForces ();
				} else {
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
				direction = Directions.none;
				disableAll ();
			}
#if DEBUG
			_SW.Stop ();
			_counter++;
			if (_counter > 200) {
				_timer = (double)_SW.ElapsedMilliseconds/_counter;
				_SW.Reset();
				_counter = 0;
			}
			if (Input.GetKeyDown (KeyCode.Space)) {
				print (String.Format("UPDATE time: {0:0.0000} ms", _timer));
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
			vectorInput.enabled = false;
			RCSForce[] forceList = (RCSForce[])GameObject.FindSceneObjectsOfType (typeof(RCSForce));
			foreach (RCSForce force in forceList) {
				Destroy (force);
			}
		}

		void recursePart (Part part, List<ModuleRCS> list)
		{
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
				vectorInput.color = Color.red;
				vectorTorque.width = 0.15f;
				vectorMovement.width = 0.08f;
			} else {
				Rotation = false;
				vectorInput.color = Color.green;
				vectorTorque.width = 0.08f;
				vectorMovement.width = 0.15f;
			}
			if (direction == dir && Rotation == rotaPrev) {
				direction = Directions.none;
			} else {
				direction = dir;
			}
		}

		void ShowCoMForces ()
		{
			/* calculate torque, translation and display them */
			RCSForce[] RCSForceList = (RCSForce[])GameObject.FindObjectsOfType (typeof(RCSForce));

			Vector3 torque = Vector3.zero;
			Vector3 translation = Vector3.zero;
			foreach (RCSForce RCSf in RCSForceList) {
				if (RCSf.vectors == null) {
					/* didn't run Start yet it seems */
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
			vectorTorque.value = torque;
			vectorMovement.value = translation;
			vectorInput.value = Normals[direction] * -1;
			if (torque.magnitude < 0.5f) {
				vectorTorque.enabled = false;
			} else {
				vectorTorque.enabled = true;
			}
		}
	}

	public class RCSForce : MonoBehaviour
	{
		/* The order must match the enum Directions */
		Vector3[] normals = new Vector3[7] {
			Vector3.zero,
			Vector3.right,
			Vector3.up,
			Vector3.forward,
			Vector3.right * -1,
			Vector3.up * -1 ,
			Vector3.forward * -1 };

		float thrustPower;
		ModuleRCS module;

		public GameObject[] vectors;
		public Directions direction;
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
			Vector3 normal = normals [(int)direction];
			Vector3 rotForce = Vector3.zero;

			if (RCSBuildAid.Rotation) {
				rotForce = Vector3.Cross (transform.position - RCSBuildAid.CoM.transform.position, normal);
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
		public float scale = 1;
		public float maxLength = 5;
		public new bool enabled = false;
		string shader = "GUI/Text Shader";

		Color _color = Color.cyan;
		float _width = 0.03f;

		GameObject arrowObj;
		LineRenderer line;
		LineRenderer arrow;

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
			float arrowL = Mathf.Clamp(dir.magnitude / 2f, 0f, width * 4);
			Vector3 pMid = pEnd - dir.normalized * arrowL;

			line.SetPosition (0, pStart);
			line.SetPosition (1, pMid);
			line.enabled = enabled;

			arrow.SetPosition(0, pMid);
			arrow.SetPosition(1, pEnd);
			arrow.enabled = enabled;
		}
	}
}
