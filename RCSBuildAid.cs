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

namespace RCSBuildAid
{
	public enum Directions { none, right, up, fwd, left, down, back };

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class RCSBuildAid : MonoBehaviour
	{

		EditorMarker_CoM CoM;
		public VectorGraphic vectorTorque, vectorMovement, vectorInput;
		public Directions direction = Directions.none;
		public GameObject[] ObjVectors = new GameObject[3];

		public Dictionary<Directions, Vector3> Normals = new Dictionary<Directions, Vector3>() {
			{ Directions.none,  Vector3.zero },
			{ Directions.right, Vector3.right },
			{ Directions.up,    Vector3.up },
			{ Directions.fwd,	Vector3.forward },
			{ Directions.left,  Vector3.right * -1 },
			{ Directions.down, 	Vector3.up * -1 },
			{ Directions.back,  Vector3.forward * -1 }
		};

		public void Awake ()
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
			vectorMovement.scale = 0.5f;
			vectorInput.color = Color.green;
		}

		public void Update ()
		{
			/* find CoM marker, we need it so we don't have to calculate the CoM ourselves */
			CoM = (EditorMarker_CoM)GameObject.FindObjectOfType (typeof(EditorMarker_CoM));
			/* find all RCS */
			ModuleRCS[] RCSList = (ModuleRCS[])GameObject.FindObjectsOfType (typeof(ModuleRCS));
			for (int i = 0; i < RCSList.Length; i++) {
				ModuleRCS RCS = RCSList [i];
				if ((CoM != null) && (direction != Directions.none)) {
					if (RCS.part.isAttached) {
						ShowForcesOf (RCS);
					}
				} else {
					/* destroy all the objects displaying the RCS forces */
					RCSForce rcsForce = RCS.part.transform.GetComponentInChildren<RCSForce> ();
					if (rcsForce != null) {
						Destroy (rcsForce.gameObject);
					}
				}
			}

			if (CoM != null) {
				if (direction != Directions.none) {
					/* display translation and torque, size of CoM proportional to 
					 * torque's magnitude */
					vectorTorque.enabled = true;
					vectorMovement.enabled = true;
					vectorInput.enabled = true;
					CoM.transform.localScale = Vector3.one * 
						Mathf.Clamp(vectorTorque.value.magnitude, 0f, 1f);
					
					/* attach our vector GameObjects to CoM */
					foreach (GameObject obj in ObjVectors) {
						if (obj.transform.parent == null) {
							obj.transform.parent = CoM.transform;
							obj.transform.localPosition = Vector3.zero;
						}
					}

					ShowTorqueForce ();
				} else {
					CoM.transform.localScale = Vector3.one;
					vectorTorque.enabled = false;
					vectorMovement.enabled = false;
					vectorInput.enabled = false;
				}

				/* Switching direction */
				if (Input.GetKeyDown (KeyCode.Space)) {
					if ((int)direction >= (int)Directions.back) {
						direction = Directions.right;
					} else {
						direction = (Directions)((int)direction + 1);
					}
				}
			} else {
				vectorTorque.enabled = false;
				vectorMovement.enabled = false;
				vectorInput.enabled = false;
				direction = Directions.none;
			}
		}
	
		public void ShowTorqueForce ()
		{
			/* calculate torque, translation and display them */
			RCSForce[] RCSForceList = (RCSForce[])GameObject.FindObjectsOfType (typeof(RCSForce));

			Vector3 torque = Vector3.zero;
			Vector3 translation = CoM.transform.position;
			foreach (RCSForce RCSf in RCSForceList) {
				Vector3 distance = RCSf.transform.position - CoM.transform.position;
				if (RCSf.vectorThrust == null) {
					/* didn't run Start yet it seems */
					continue;
				}
				for (int t = 0; t < RCSf.vectorThrust.Length; t++) {
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

		/* Show the forces that a RCS part can do */
		public void ShowForcesOf (ModuleRCS mod)
		{
			RCSForce force = mod.part.transform.GetComponentInChildren<RCSForce> ();
			if (force == null) {
				GameObject RCSForceObject = new GameObject("RCSForces");
				force = RCSForceObject.AddComponent<RCSForce> ();
				force.Init (mod);
			}
			force.direction = direction;
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
		GameObject[] vectors;

		public Directions direction;
		public Vector3[] vectorThrust;

		public void Init (ModuleRCS module)
		{
			/* this method works as a sort of Awake
			 * Module is not fully loaded here! */
			transform.parent = module.transform;
			transform.localPosition = Vector3.zero;
			this.module = module;
		}

		public void Start ()
		{
			if (module == null) {
				/* this seems to happen when using symmetry
				 * for some reason the ref is lost or something? */
				Destroy (gameObject);
				return;
			}
			/* Module loaded here */
			int n = module.thrusterTransforms.Count;
			vectorThrust = new Vector3[n];
			vectors = new GameObject[n];
			for (int i = 0; i < n; i++) {
				vectors [i] = new GameObject ("RCSVector");
				vectors [i].transform.parent = transform;
				vectors [i].transform.localPosition = Vector3.zero;
				vectors [i].AddComponent<VectorGraphic> ();
				vectorThrust [i] = Vector3.zero;
			}
			thrustPower = module.thrusterPower;
		}

		public void Update ()
		{
			float force;
			VectorGraphic vector;
			Vector3 thrust;
			Vector3 normal = normals [(int)direction];

			/* calculate the force  */
			for (int t = 0; t < module.thrusterTransforms.Count; t++) {
				thrust = module.thrusterTransforms[t].up;
				force = Mathf.Max (Vector3.Dot (thrust, normal), 0f) * thrustPower;
				vectorThrust[t] = thrust * force;

				/* update VectorGraphic */
				vector = vectors[t].GetComponent<VectorGraphic> ();
				vector.value = vectorThrust[t];
				/* show it if there's force */
				if (force > 0f) {
					vector.enabled = true;
				} else {
					vector.enabled = false;
				}
			}
		}
	}

	public class VectorGraphic : MonoBehaviour
	{
		public Vector3 value = Vector3.zero;
		public Color color = Color.cyan;
		public float width = 0.03f;
		public float scale = 1;
		public new bool enabled = false;
		string shader = "GUI/Text Shader";

		GameObject arrowObj = new GameObject("GraphicVectorArrow");
		LineRenderer line;
		LineRenderer arrow;

		public void Awake ()
		{
			/* try GetComponent fist, symmetry can add LineRenderer
			 * beforehand and we would get an error with AddComponent
			 */
 			line = GetComponent<LineRenderer> ();
			if (line == null)
				line = gameObject.AddComponent<LineRenderer> ();
			line.SetVertexCount (2);
			line.material = new Material (Shader.Find (shader));
				
			/* arrow point */
			arrowObj.transform.parent = transform;
			arrowObj.transform.localPosition = Vector3.zero;
			arrow = arrowObj.GetComponent<LineRenderer> ();
			if (arrow == null) {
				arrow = arrowObj.AddComponent<LineRenderer> ();
			}
			arrow.SetVertexCount(2);
			arrow.material = line.material;

			line.enabled = false;
			arrow.enabled = false;
		}

		public void Start ()
		{
			arrow.SetColors(color, color);
			line.SetColors(color, color);
			line.SetWidth (width, width);
			arrow.SetWidth(width * 3, 0);
		}

		public void Update ()
		{
			Vector3 pStart = transform.position;
			Vector3 pEnd = (pStart + value) * scale;
			Vector3 dir = pEnd - pStart;
			/* calculate arrow tip lenght */
			float arrowL = Mathf.Clamp(dir.magnitude / 2f, 0f, width * 4);
			Vector3 pMid = pEnd - dir.normalized * arrowL;

			line.SetPosition (0, pStart);
			line.SetPosition (1, pMid);

			arrow.SetPosition(0, pMid);
			arrow.SetPosition(1, pEnd);

			line.enabled = enabled;
			arrow.enabled = enabled;
		}
	}
}