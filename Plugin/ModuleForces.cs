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
        public new bool enabled = true;

        protected Color color = Color.cyan;

        protected virtual void Init ()
        {
        }

        protected void Awake ()
        {
            Init ();
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
            }
        }

        protected virtual void Update ()
        {
            if (RCSBuildAid.ReferenceMarker == null) {
                return;
            }

            if (RCSBuildAid.Enabled && Enabled && connectedToVessel) {
                Enable ();
            } else {
                Disable ();
            }
        }

        public void Enable ()
        {
            if (!enabled) {
                enabled = true;
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = true;
                }
            }
        }

        public void Disable ()
        {
            if (enabled) {
                enabled = false;
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = false;
                }
            }
        }

        protected abstract bool connectedToVessel { get; }
        protected abstract bool Enabled { get; }
        protected abstract List<Transform> thrustTransforms { get; }
    }

    /* Component for calculate and show forces in RCS */
    public class RCSForce : ModuleForces
    {
        ModuleRCS module;

        #region implemented abstract members of ModuleForces
        protected override bool Enabled {
            get {
                switch (RCSBuildAid.Mode) {
                case PluginMode.RCS:
                case PluginMode.Attitude:
                    return true;
                case PluginMode.Engine:
                    return Settings.eng_include_rcs;
                }
                return false;
            }
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrusterTransforms; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.RCS.Contains (module); }
        }
        #endregion

        protected override void Init ()
        {
            module = GetComponent<ModuleRCS> ();
            if (module == null) {
                throw new Exception ("Missing ModuleRCS component.");
            }
        }

        bool controlAttitude {
            get {
                switch (RCSBuildAid.Mode) {
                case PluginMode.Attitude:
                    return true;
                case PluginMode.Engine:
                    return Settings.eng_include_rcs;
                }
                return false;
            }
        }

        protected override void Update ()
        {
            base.Update ();

            VectorGraphic vector;
            Transform thrusterTransform;
            float magnitude;
            Vector3 thrustDirection;
            Vector3 directionVector = RCSBuildAid.TranslationVector;

            /* calculate forces applied in the specified direction  */
            for (int t = 0; t < module.thrusterTransforms.Count; t++) {
                vector = vectors [t];
                thrusterTransform = module.thrusterTransforms [t];
                if (!module.rcsEnabled || (thrusterTransform.position == Vector3.zero)) {
                    vector.value = Vector3.zero;
                    vector.enabled = false;
                    continue;
                }
                if (controlAttitude) {
                    Vector3 lever = thrusterTransform.position - RCSBuildAid.ReferenceMarker.transform.position;
                    directionVector = Vector3.Cross (lever.normalized, RCSBuildAid.RotationVector) * -1;
                }
                thrustDirection = thrusterTransform.up;
                magnitude = Mathf.Max (Vector3.Dot (thrustDirection, directionVector), 0f);
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

    /* Component for calculate and show forces in engines */
    public class EngineForce : ModuleForces
    {
        ModuleEngines module;

        #region implemented abstract members of ModuleForces
        protected override bool Enabled {
            get {
                switch (RCSBuildAid.Mode) {
                case PluginMode.Engine:
                    return true;
                }
                return false;
            }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrustTransforms; }
        }
        #endregion

        protected virtual Part Part {
            get { return module.part; }
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

        protected override void Init ()
        {
            module = GetComponent<ModuleEngines> ();
            if (module == null) {
                throw new Exception ("Missing ModuleEngines component.");
            }
            GimbalRotation.addTo (gameObject);
        }

        protected override void Update ()
        {
            base.Update ();

            float thrust = getThrust ();
            for (int i = 0; i < vectors.Length; i++) {
                if (Part.inverseStage == RCSBuildAid.LastStage) {
                    Transform t = thrustTransforms [i];
                    /* RCS use the UP vector for direction of thrust, but no, engines use forward */
                    vectors [i].value = t.forward * thrust;
                } else {
                    vectors [i].value = Vector3.zero;
                }
            }
        }
    }

    public class EnginesFXForce : EngineForce
    {
        ModuleEnginesFX module;

        protected override void Init ()
        {
            module = GetComponent<ModuleEnginesFX> ();
            if (module == null) {
                throw new Exception ("Missing ModuleEnginesFX component.");
            }
            GimbalRotation.addTo (gameObject);
        }
        /* need to override anything that uses module due to being of a different type */
        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        protected override List<Transform> thrustTransforms {
            get { return module.thrustTransforms; }
        }

        protected override Part Part {
            get { return module.part; }
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

    /* Component for calculate and show forces in engines such as RAPIER */
    public class MultiModeEngineForce : EngineForce
    {
        MultiModeEngine module;
        Dictionary<string, ModuleEnginesFX> modes = new Dictionary<string, ModuleEnginesFX> ();

        ModuleEnginesFX activeMode {
            get { return modes[module.mode]; }
        }

        protected override List<Transform> thrustTransforms {
            get { return activeMode.thrustTransforms; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        protected override Part Part {
            get { return module.part; }
        }

        protected override float getThrust ()
        {
            float maxThrust = activeMode.maxThrust / thrustTransforms.Count;
            float minThrust = activeMode.minThrust / thrustTransforms.Count;
            float p = activeMode.thrustPercentage / 100;
            float thrust = (maxThrust - minThrust) * p + minThrust;
            return thrust;
        }

        protected override void Init ()
        {
            module = GetComponent<MultiModeEngine> ();
            if (module == null) {
                throw new Exception ("Missing MultiModeEngine component.");
            }
            ModuleEnginesFX[] engines = module.GetComponents<ModuleEnginesFX> ();
            foreach (ModuleEnginesFX eng in engines) {
                modes [eng.engineID] = eng;
            }
            GimbalRotation.addTo (gameObject);
        }
    }
}
