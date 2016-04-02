/* Copyright © 2013-2016, Elián Hanisch <lambdae2@gmail.com>
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
    public class MarkerForces : MonoBehaviour
    {
        MarkerVectorGraphic transVector;
        MarkerVectorGraphic torqueVector;
        CircularVectorGraphic torqueCircle;
        Vector3 torque = Vector3.zero;
        Vector3 translation = Vector3.zero;
        float twr;
        GameObject marker;
        MassEditorMarker comm;

        public MomentOfInertia MoI;

        public GameObject Marker {
            get { return marker; }
            set {
                marker = value;
                comm = marker.GetComponent<MassEditorMarker> ();
            }
        }

        public Vector3 Thrust () {
            return translation;
        }

        public Vector3 Torque () {
            return torque;
        }

        public float TWR {
            get { return twr; } 
        }

        [Obsolete("Use Thrust () if possible.")]
        public Vector3 Thrust (MarkerType reference)
        {
            Vector3 thrust, torque;
            GameObject marker = RCSBuildAid.GetMarker(reference);
            calcMarkerForces (marker.transform, out thrust, out torque);
            return thrust;
        }

        [Obsolete("Use Torque () if possible.")]
        public Vector3 Torque (MarkerType reference)
        {
            Vector3 thrust, torque;
            GameObject marker = RCSBuildAid.GetMarker(reference);
            calcMarkerForces (marker.transform, out thrust, out torque);
            return torque;
        }

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                if (torqueCircle == null) {
                    return;
                }
                transVector.gameObject.SetActive (value);
                torqueCircle.gameObject.SetActive (value);
                torqueVector.gameObject.SetActive (value);
            }
        }

        GameObject getGameObject (string name)
        {
            var obj = new GameObject (name);
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            return obj;
        }

        void Awake ()
        {
            /* layer change must be done before adding the Graphic components */
            transVector = getGameObject ("Translation Vector Object").AddComponent<MarkerVectorGraphic> ();
            Color color = Color.green;
            color.a = 0.4f;
            transVector.setColor(color);

            torqueVector = getGameObject ("Torque Vector Object").AddComponent<MarkerVectorGraphic> ();
            color = XKCDColors.ReddishOrange;
            color.a = 0.6f;
            torqueVector.setColor(color);

            torqueCircle = getGameObject ("Torque Circle Object").AddComponent<CircularVectorGraphic> ();

            MoI = gameObject.AddComponent<MomentOfInertia> ();
        }

        void OnDestroy ()
        {
        }

        void Start ()
        {
            if (RCSBuildAid.ReferenceMarker == Marker) {
                /* we should start enabled */
                enabled = true;
            } else {
                enabled = false;
            }
        }

        Vector3 calcTorque (Transform forceTransform, Transform pivotTransform, Vector3 force)
        {
            Vector3 lever = pivotTransform.position - forceTransform.position;
            return Vector3.Cross (lever, force);
        }

        void sumForces (List<PartModule> moduleList, Transform refTransform, 
                        ref Vector3 translation, ref Vector3 torque)
        {
            for (int i = 0; i < moduleList.Count; i++) {
                PartModule mod = moduleList [i];
                if (mod == null) {
                    continue;
                }
                ModuleForces mf = mod.GetComponent<ModuleForces> ();
                if (mf == null || !mf.enabled) {
                    continue;
                }
                for (int t = 0; t < mf.vectors.Length; t++) {
                    /* vectors represent exhaust force, so -1 for actual thrust */
                    Vector3 force = -1 * mf.vectors [t].value;
                    translation += force;
                    torque += calcTorque (mf.vectors [t].transform, refTransform, force);
                }
            }
        }

        void LateUpdate ()
        {
            if (Marker == null) {
                return;
            }
            bool enabled, visible;
            if (!RCSBuildAid.Enabled) {
                enabled = false;
            } else if (RCSBuildAid.Mode == PluginMode.none) {
                enabled = false;
            } else {
                enabled = Marker.activeInHierarchy;
            }
            visible = enabled && Marker.GetComponent<Renderer> ().enabled;

            /* show vectors if visible */
            if (transVector.enabled != visible) {
                transVector.enabled = visible;
                torqueVector.enabled = visible;
                torqueCircle.enabled = visible;
            }
            if (!enabled) {
                transVector.value = Vector3.zero;
                torqueVector.value = Vector3.zero;
                torqueCircle.value = Vector3.zero;
                return;
            }
            transform.position = Marker.transform.position;

            /* calculate torque, translation and display them */
            calcMarkerForces (Marker.transform, out translation, out torque);

            /* update vectors in CoM */
            torqueVector.value = torque;
            transVector.value = translation; /* NOTE: in engine mode this is overwriten below */
            twr = translation.magnitude / (comm.mass * Settings.selected_body.ASLGravity ());

            switch (RCSBuildAid.Mode) {
            case PluginMode.RCS:
                /* translation mode, we want to reduce torque */
                transVector.valueTarget = RCSBuildAid.TranslationVector * -1;
                torqueVector.valueTarget = Vector3.zero;
                break;
            case PluginMode.Attitude:
                /* rotation mode, we want to reduce translation */
                torqueVector.valueTarget = RCSBuildAid.RotationVector;
                transVector.valueTarget = Vector3.zero;
                break;
            case PluginMode.Engine:
                torqueVector.valueTarget = Vector3.zero;
                /* make it proportional to TWR */
                transVector.value = translation.normalized * twr * 5/3;
                switch (EditorDriver.editorFacility) {
                case EditorFacility.VAB:
                    transVector.valueTarget = Vector3.up;
                    break;
                case EditorFacility.SPH:
                    transVector.valueTarget = Vector3.forward;
                    break;
                default:
                    transVector.valueTarget = Vector3.zero;
                    break;
                }
                break;
            }

            if (torque != Vector3.zero) {
                // Analysis disable once CompareOfFloatsByEqualityOperator
                if (MoI.value == 0) {
                    /* this only happens with single part crafts, because all mass is concentrated
                     * in the CoM, so lets just use torque */
                    torqueCircle.value = torque;
                } else {
                    torqueCircle.value = torque / MoI.value;
                }
                torqueCircle.transform.rotation = Quaternion.LookRotation (torque, translation);
            } else {
                torqueCircle.value = Vector3.zero;
            }
        }

        void calcMarkerForces (Transform position, out Vector3 translation, out Vector3 torque)
        {
            torque = Vector3.zero;
            translation = Vector3.zero;

            switch (RCSBuildAid.Mode) {
            case PluginMode.Parachutes:
                torque = calcTorque (RCSBuildAid.CoD.transform, 
                    RCSBuildAid.ReferenceMarker.transform,
                    CoDMarker.DragForce);
                break;
            default:
                sumForces (RCSBuildAid.RCS, position, ref translation, ref torque);
                sumForces (RCSBuildAid.Engines, position, ref translation, ref torque);
                break;
            }
        }
    }
}

