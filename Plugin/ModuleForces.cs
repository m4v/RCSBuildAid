/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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
    public abstract class ModuleForces : MonoBehaviour
    {
        public VectorGraphic[] vectors = new VectorGraphic[0];

        const int layer = 1;
        PartModule module;

        protected Color color = Color.cyan;

        protected virtual void Awake (PartModule module)
        {
            this.module = module;
            gameObject.layer = layer;
        }

        protected Part Part {
            get { return module.part; }
        }

        protected virtual void Start ()
        {
            /* thrusterTransforms aren't initialized while in Awake, so in Start instead */
            GameObject obj;
            if (vectors.Length > 0) {
                /* clonned by symmetry, do nothing */
                return;
            }
            int n = thrustTransforms.Count;
            vectors = new VectorGraphic[n];
            for (int i = 0; i < n; i++) {
                obj = new GameObject ("PartModule Vector object");
                obj.layer = gameObject.layer;
                obj.transform.parent = transform;
                obj.transform.position = thrustTransforms [i].position;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
                vectors [i].setColor(color);
                vectors [i].setLayer (gameObject.layer);
            }
        }

        protected virtual void Update ()
        {
            if (RCSBuildAid.ReferenceMarker == null) {
                return;
            }

            if (!RCSBuildAid.Enabled || !moduleList.Contains(module)) {
                Disable ();
                return;
            }
        }

        public void Enable ()
        {
            enabled = true;
            for (int i = 0; i < vectors.Length; i++) {
                vectors [i].enabled = true;
            }
        }

        public void Disable ()
        {
            enabled = false;
            for (int i = 0; i < vectors.Length; i++) {
                vectors [i].enabled = false;
            }
        }

        protected abstract List<Transform> thrustTransforms { get; }

        protected abstract List<PartModule> moduleList { get; }
    }

    /* Component for calculate and show forces in RCS */
    public class RCSForce : ModuleForces
    {
        ModuleRCS module;

        protected override List<PartModule> moduleList {
            get { return RCSBuildAid.RCSlist; }
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrusterTransforms; }
        }

        void Awake ()
        {
            module = GetComponent<ModuleRCS> ();
            if (module == null) {
                throw new Exception ("Missing ModuleRCS component.");
            }
            base.Awake (module);
        }

        protected override void Update ()
        {
            base.Update ();

            VectorGraphic vector;
            Transform thrusterTransform;
            float magnitude;
            Vector3 thrustDirection;
            Vector3 normal = RCSBuildAid.Normal;

            /* calculate forces applied in the specified direction  */
            for (int t = 0; t < module.thrusterTransforms.Count; t++) {
                vector = vectors [t];
                thrusterTransform = module.thrusterTransforms [t];
                if (!module.rcsEnabled || (thrusterTransform.position == Vector3.zero)) {
                    vector.value = Vector3.zero;
                    vector.enabled = false;
                    continue;
                }
                if (RCSBuildAid.mode == PluginMode.Attitude) {
                    Vector3 lever = thrusterTransform.position - RCSBuildAid.ReferenceMarker.transform.position;
                    normal = Vector3.Cross (lever.normalized, RCSBuildAid.Normal);
                }
                thrustDirection = thrusterTransform.up;
                magnitude = Mathf.Max (Vector3.Dot (thrustDirection, normal), 0f);
                magnitude = Mathf.Clamp (magnitude, 0f, 1f) * module.thrusterPower;
                Vector3 vectorThrust = thrustDirection * magnitude;

                /* update VectorGraphic */
                vector.value = vectorThrust;
                /* show it if there's force */
                if (enabled) {
                    vector.enabled = (magnitude > 0f);
                }
            }
        }
    }

    public class GimbalRotation : MonoBehaviour
    {
        ModuleGimbal gimbal;
        [SerializeField] // need this for not mess up gimbals of mirrored parts
        Quaternion[] initRots;
        [SerializeField]
        float startTime;

        const float speed = 2f;

        void Awake ()
        {
            RCSBuildAid.events.onDirectionChange += switchDirection;
        }

        void OnDestroy ()
        {
            RCSBuildAid.events.onDirectionChange -= switchDirection;
        }

        void Start ()
        {
            // FIXME modded parts can have more than one gimbal module
            gimbal = GetComponent<ModuleGimbal> ();
            if (gimbal != null && initRots == null) {
                initRots = new Quaternion[gimbal.gimbalTransforms.Count];
                for (int i = 0; i < gimbal.gimbalTransforms.Count; i++) {
                    initRots [i] = gimbal.gimbalTransforms [i].localRotation;
                }
            }
        }

        void switchDirection (Direction direction)
        {
            /* for the animation */
            startTime = Time.time;
        }

        void Update ()
        {
            if (gimbal == null) {
                return;
            }
            for (int i = 0; i < gimbal.gimbalTransforms.Count; i++) {
                Transform t = gimbal.gimbalTransforms [i];
                Quaternion finalRotation;
                if (gimbal.gimbalLock || (gimbal.part.inverseStage != RCSBuildAid.lastStage)) {
                    finalRotation = initRots [i];
                } else {
                    float angle = gimbal.gimbalRange;
                    Vector3 pivot;
                    switch (RCSBuildAid.Direction) {
                    /* forward and back are the directions for roll when in attitude modes */
                    case Direction.forward:
                        angle *= -1; /* roll left */
                        goto roll_calc;
                    case Direction.back:
                        roll_calc:
                        Vector3 vessel_up = RCSBuildAid.AttitudeVector;
                        Vector3 dist = t.position - RCSBuildAid.ReferenceMarker.transform.position;
                        pivot = dist - Vector3.Dot (dist, vessel_up) * vessel_up;
                        if (pivot.sqrMagnitude > 0.01) {
                            pivot = t.InverseTransformDirection (pivot);
                            finalRotation = initRots [i] * Quaternion.AngleAxis (angle, pivot);
                        } else {
                            finalRotation = initRots [i];
                        }
                        break;
                    default:
                        pivot = t.InverseTransformDirection (RCSBuildAid.AttitudeVector);
                        finalRotation = initRots [i] * Quaternion.AngleAxis (angle, pivot);
                        break;
                    }
                }
                t.localRotation = Quaternion.Lerp (t.localRotation, finalRotation, (Time.time - startTime) * speed);
            }
        }
    }

    /* Component for calculate and show forces in engines */
    public class EngineForce : ModuleForces
    {
        ModuleEngines module;

        protected override List<PartModule> moduleList {
            get { return RCSBuildAid.EngineList; }
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrustTransforms; }
        }

        protected virtual float getThrust ()
        {
            float maxThrust = module.maxThrust / thrustTransforms.Count;
            float minThrust = module.minThrust / thrustTransforms.Count;
            float p = module.thrustPercentage / 100;
            float thrust = (maxThrust - minThrust) * p + minThrust;
            return thrust;
        }

        protected override void Start ()
        {
            color = Color.yellow;
            color.a = 0.75f;
            base.Start ();
            for (int i = 0; i < vectors.Length; i++) {
                vectors [i].upperMagnitude = 1500f;
                vectors [i].lowerMagnitude = 0.025f;
                vectors [i].maxLength = 4f;
                vectors [i].minLength = 0.5f;
                vectors [i].maxWidth = 0.2f;
                vectors [i].minWidth = 0.04f;
            }
        }

        void Awake ()
        {
            module = GetComponent<ModuleEngines> ();
            if (module == null) {
                throw new Exception ("Missing ModuleEngines component.");
            }
            Awake (module);
        }

        protected override void Awake (PartModule module)
        {
            if (gameObject.GetComponent<GimbalRotation> () == null) {
                gameObject.AddComponent<GimbalRotation> ();
            }
            base.Awake (module);
        }

        protected override void Update ()
        {
            base.Update ();

            float thrust = getThrust ();
            for (int i = 0; i < vectors.Length; i++) {
                if (Part.inverseStage == RCSBuildAid.lastStage) {
                    Transform t = thrustTransforms [i];
                    /* RCS use the UP vector for direction of thrust, but no, engines use forward */
                    vectors [i].value = t.forward * thrust;
                } else {
                    vectors [i].value = Vector3.zero;
                }
            }
        }
    }

    /* Component for calculate and show forces in engines such as RAPIER */
    public class MultiModeEngineForce : EngineForce
    {
        MultiModeEngine module;
        Dictionary<string, ModuleEnginesFX> modes = new Dictionary<string, ModuleEnginesFX> ();

        ModuleEnginesFX activeMode {
            get { return modes[module.mode]; }
        }

        protected override List<PartModule> moduleList {
            get { return RCSBuildAid.EngineList; }
        }

        protected override List<Transform> thrustTransforms {
            get { return activeMode.thrustTransforms; }
        }

        protected override float getThrust ()
        {
            float maxThrust = activeMode.maxThrust / thrustTransforms.Count;
            float minThrust = activeMode.minThrust / thrustTransforms.Count;
            float p = activeMode.thrustPercentage / 100;
            float thrust = (maxThrust - minThrust) * p + minThrust;
            return thrust;
        }

        void Awake ()
        {
            module = GetComponent<MultiModeEngine> ();
            if (module == null) {
                throw new Exception ("Missing MultiModeEngine component.");
            }
            ModuleEnginesFX[] engines = module.GetComponents<ModuleEnginesFX> ();
            foreach (ModuleEnginesFX eng in engines) {
                modes [eng.engineID] = eng;
            }
            Awake (module);
        }
    }

    public class EnginesFXForce : EngineForce
    {
        ModuleEnginesFX module;

        void Awake ()
        {
            module = GetComponent<ModuleEnginesFX> ();
            if (module == null) {
                throw new Exception ("Missing ModuleEnginesFX component.");
            }
            Awake (module);
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrustTransforms; }
        }

        protected override float getThrust ()
        {
            float maxThrust = module.maxThrust / thrustTransforms.Count;
            float minThrust = module.minThrust / thrustTransforms.Count;
            float p = module.thrustPercentage / 100;
            float thrust = (maxThrust - minThrust) * p + minThrust;
            return thrust;
        }
    }
}
