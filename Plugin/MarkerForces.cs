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
using UnityEngine.Profiling;

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

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                transVector.gameObject.SetActive (value);
                torqueCircle.gameObject.SetActive (value);
                torqueVector.gameObject.SetActive (value);
            }
        }

        GameObject getGameObject (string objectName)
        {
            var obj = new GameObject (objectName);
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            return obj;
        }

        void Awake ()
        {
#if DEBUG
            Debug.Log("[RCSBA]: MarkerForces Awake");
#endif
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
#if DEBUG
            Debug.Log("[RCSBA]: MarkerForces Destroy");
#endif
        }

        void Start ()
        {
#if DEBUG
            Debug.Log("[RCSBA]: MarkerForces Start");
#endif
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

        void sumForces (IList<ModuleForces> forceList, Transform refTransform, ref Vector3 translation, ref Vector3 torque)
        {
            for (int i = forceList.Count - 1; i >= 0; i--) {
                ModuleForces mforces = forceList [i];
                
                Debug.Assert(mforces != null, "[RCSBA, MarkerForces]: moduleforces != null");
                
                if (!mforces.enabled) {
                    continue;
                }
                for (int t = mforces.vectors.Length - 1; t >= 0; t--) {
                    /* vectors represent exhaust force, so negative for actual thrust */
                    Vector3 force = -1 * mforces.vectors [t].value;
                    translation += force;
                    torque += calcTorque (mforces.vectors [t].transform, refTransform, force);
                }
            }
        }

        void LateUpdate ()
        {
            Debug.Assert(Marker != null, "[RCSBA, MarkerForces]: Marker != null");
            Profiler.BeginSample("[RCSBA] MarkerForces LateUpdate");
            
            bool enabled;
            if (!RCSBuildAid.Enabled) {
                enabled = false;
            } else if (RCSBuildAid.Mode == PluginMode.none) {
                enabled = false;
            } else {
                enabled = Marker.activeInHierarchy;
            }
            bool visible = enabled && Marker.GetComponent<Renderer> ().enabled;

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
                Profiler.EndSample();
                return;
            }
            transform.position = Marker.transform.position;

            Profiler.BeginSample("[RCSBA] MarkerForces calcMarkerForces");
            /* calculate torque, translation and display them */
            calcMarkerForces (Marker.transform, out translation, out torque);
            Profiler.EndSample();

            /* update vectors in CoM */
            torqueVector.value = torque;
            transVector.value = translation; /* NOTE: in engine mode this is overwritten below */
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
                /* make it proportional to TWR. Max length at TWR of 3 */
                transVector.value = translation.normalized * (twr * transVector.maximumMagnitude / 3f);
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
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (MoI.value == 0) {
                    /* this only happens with single part crafts, because all mass is concentrated
                     * in the CoM, so lets just use torque */
                    torqueCircle.value = torque;
                } else {
                    /* circular vector is proportional to angular speed, not torque */
                    torqueCircle.value = torque / MoI.value;
                }
                torqueCircle.transform.rotation = Quaternion.LookRotation (torque, translation);
            } else {
                torqueCircle.value = Vector3.zero;
            }
            Profiler.EndSample();
        }

        void calcMarkerForces (Transform position, out Vector3 translation, out Vector3 torque)
        {
            torque = Vector3.zero;
            translation = Vector3.zero;

            switch (RCSBuildAid.Mode) {
            case PluginMode.Parachutes:
                torque = calcTorque (RCSBuildAid.CoD.transform,
                    RCSBuildAid.ReferenceMarker.transform, CoDMarker.DragForce);
                break;
            default:
                sumForces (ModuleForces.List, position, ref translation, ref torque);
                break;
            }
        }
    }
}

