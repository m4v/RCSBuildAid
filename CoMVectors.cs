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
    /* Component for calculate and show forces in CoM */
    public class CoMVectors : MonoBehaviour
    {
        VectorGraphic transVector;
        TorqueGraphic torqueCircle;
        float threshold = 0.01f;
        Vector3 torque = Vector3.zero;
        Vector3 translation = Vector3.zero;

        public GameObject Marker;
        public MomentOfInertia MoI;

        public float valueTorque {
            get { 
                if (torqueCircle == null) {
                    return 0f;
                }
                return torqueCircle.value.magnitude;
            }
        }

        public float valueTranslation {
            get {
                if (transVector == null) {
                    return 0f;
                }
                return transVector.value.magnitude; 
            }
        }

        public Vector3 thrust {
            get {
                return transVector == null ? Vector3.zero : transVector.value * -1;
            }
        }

        public Vector3 Torque {
            get {
                return transVector == null ? Vector3.zero : torqueCircle.vector.value;
            }
        }

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                if (transVector == null || torqueCircle == null) {
                    return;
                }
                transVector.gameObject.SetActive (value);
                torqueCircle.gameObject.SetActive (value);
            }
        }

        void Awake ()
        {
            /* layer change must be done before adding the Graphic components */
            GameObject obj = new GameObject ("Translation Vector Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            transVector = obj.AddComponent<VectorGraphic> ();
            Color color = Color.green;
            color.a = 0.6f;
            transVector.color = color;
            transVector.offset = 0.6f;
            transVector.maxLength = 3f;
            transVector.minLength = 0.25f;
            transVector.maxWidth = 0.16f;
            transVector.minWidth = 0.05f;
            transVector.upperMagnitude = 5;
            transVector.lowerMagnitude = threshold;
            transVector.exponentialScale = true;

            obj = new GameObject ("Torque Circle Object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            torqueCircle = obj.AddComponent<TorqueGraphic> ();
            torqueCircle.vector.offset = 0.6f;
            torqueCircle.vector.maxLength = 3f;
            torqueCircle.vector.minLength = 0.25f;
            torqueCircle.vector.maxWidth = 0.16f;
            torqueCircle.vector.minWidth = 0.05f;
            torqueCircle.vector.upperMagnitude = 5;
            torqueCircle.vector.lowerMagnitude = threshold;
            torqueCircle.vector.exponentialScale = true;

            MoI = gameObject.AddComponent<MomentOfInertia> ();
        }

        void Start ()
        {
            if (RCSBuildAid.Reference == Marker) {
                /* we should start enabled */
                enabled = true;
            } else {
                enabled = false;
            }
        }

        Vector3 calcTorque (Transform transform, Vector3 force)
        {
            Vector3 lever = transform.position - this.transform.position;
            return Vector3.Cross (lever, force);
        }

        void sumForces (List<PartModule> moduleList)
        {
            foreach (PartModule mod in moduleList) {
                if (mod == null) {
                    continue;
                }
                ModuleForces mf = mod.GetComponent<ModuleForces> ();
                if (mf == null || !mf.enabled) {
                    continue;
                }
                for (int t = 0; t < mf.vectors.Length; t++) {
                    Vector3 force = mf.vectors [t].value;
                    translation -= force;
                    torque += calcTorque (mf.vectors [t].transform, force);
                }
            }
        }

        void LateUpdate ()
        {
            if (Marker == null) {
                return;
            }
            bool enabled;
            if (RCSBuildAid.mode == DisplayMode.none) {
                enabled = false;
            } else {
                enabled = Marker.activeInHierarchy && Marker.renderer.enabled;
            }

            /* we need to do this because this object isn't parented to the marker */
            if (transVector.enabled != enabled) {
                transVector.enabled = enabled;
            }
            if (torqueCircle.enabled != enabled) {
                torqueCircle.enabled = enabled;
            }
            if (!enabled) {
                return;
            }
            transform.position = Marker.transform.position;
            /* calculate torque, translation and display them */
            torque = Vector3.zero;
            translation = Vector3.zero;

            switch(RCSBuildAid.mode) {
            case DisplayMode.RCS:
                sumForces (RCSBuildAid.RCSlist);
                if (RCSBuildAid.rcsMode == RCSMode.ROTATION) {
                    /* rotation mode, we want to reduce translation */
                    torqueCircle.valueTarget = RCSBuildAid.Normal * -1;
                    transVector.valueTarget = Vector3.zero;
                } else {
                    /* translation mode, we want to reduce torque */
                    transVector.valueTarget = RCSBuildAid.Normal * -1;
                    torqueCircle.valueTarget = Vector3.zero;
                }
                break;
            case DisplayMode.Engine:
                sumForces (RCSBuildAid.EngineList);
                torqueCircle.valueTarget = Vector3.zero;
                transVector.valueTarget = Vector3.zero;
                break;
            }

            /* update vectors in CoM */
            torqueCircle.value = torque;
            transVector.value = translation;

            if (torque != Vector3.zero) {
                if (MoI.value == 0) {
                    /* this only happens with single part crafts, because all mass is concentrated
                     * in the CoM, so lets just use torque */
                    torqueCircle.valueCircle = torque;
                } else {
                    torqueCircle.valueCircle = torque / MoI.value;
                }
                torqueCircle.transform.rotation = Quaternion.LookRotation (torque, translation);
            } else {
                torqueCircle.valueCircle = Vector3.zero;
            }
        }
    }
}

