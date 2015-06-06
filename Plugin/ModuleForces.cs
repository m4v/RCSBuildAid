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

        protected Color color = Color.cyan;

        protected virtual void Init ()
        {
        }

        protected void Awake ()
        {
            Init ();
            Events.LeavingEditor += onLeavingEditor;
            Events.PluginDisabled += onPluginDisabled;
            Events.PluginEnabled += onPluginEnabled;
            Events.PartChanged += onPartChanged;
            RCSBuildAid.events.ModeChanged += onModeChanged;
        }

        void OnDestroy()
        {
            Events.LeavingEditor -= onLeavingEditor;
            Events.PluginDisabled -= onPluginDisabled;
            Events.PluginEnabled -= onPluginEnabled;
            Events.PartChanged -= onPartChanged;
            RCSBuildAid.events.ModeChanged -= onModeChanged;
        }

        void onLeavingEditor ()
        {
            Disable ();
        }

        void onPluginDisabled()
        {
            Disable ();
        }

        void onPluginEnabled()
        {
            stateChanged ();
        }

        void onModeChanged (PluginMode mode)
        {
            stateChanged ();
        }

        void onPartChanged ()
        {
            stateChanged ();
        }

        void stateChanged ()
        {
            if (RCSBuildAid.Enabled && activeInMode (RCSBuildAid.Mode) && connectedToVessel) {
                Enable ();
            } else {
                Disable ();
            }
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
            stateChanged (); /* activate module if needed */
        }

        protected virtual void Update ()
        {
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
        protected abstract bool activeInMode (PluginMode mode);
        protected abstract List<Transform> thrustTransforms { get; }
    }

    /* Component for calculate and show forces in RCS */
    public class RCSForce : ModuleForces
    {
        ModuleRCS module;

        #region implemented abstract members of ModuleForces
        protected override bool activeInMode (PluginMode mode)
        {
            switch (mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
                return true;
            case PluginMode.Engine:
                return Settings.eng_include_rcs;
            }
            return false;
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
            if (!enabled) {
                return;
            }

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
        protected override bool activeInMode (PluginMode mode)
        {
            switch (mode) {
            case PluginMode.Engine:
                return true;
            }
            return false;
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        protected override List<Transform> thrustTransforms {
            get { return Engine.thrustTransforms; }
        }
        #endregion

        protected virtual ModuleEngines Engine { 
            get { return module; }
        }

        protected virtual Part Part {
            get { return Engine.part; }
        }

        protected virtual float maxThrust {
            get { return Engine.maxThrust / thrustTransforms.Count; }
        }

        protected virtual float minThrust {
            get { return Engine.minThrust / thrustTransforms.Count; }
        }

        protected virtual float vacIsp {
            get { return Engine.atmosphereCurve.Evaluate(0); }
        }

        protected virtual float getThrust ()
        {
            float p = Engine.thrustPercentage / 100;
            return Mathf.Lerp (minThrust, maxThrust, p);
        }

        protected virtual float getAtmIsp (float pressure) {
            float isp = Engine.atmosphereCurve.Evaluate (pressure * (float)PhysicsGlobals.KpaToAtmospheres);
            return isp;
        }

        protected virtual float getThrust(bool ASL)
        {
            float vac_thrust = getThrust ();
            float pressure = 0f;
            float density = 0f;
            float n = 1f;
            if (ASL) {
                pressure = Settings.selected_body.ASLPressure ();
                density = Settings.selected_body.ASLDensity ();
            }
            float atm_isp = getAtmIsp (pressure);
            if (Engine.atmChangeFlow) {
                n = density / 1.225f;
                if (Engine.useAtmCurve) {
                    n = Engine.atmCurve.Evaluate (n);
                }
            }
            return vac_thrust * n * atm_isp / vacIsp;
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
            if (!enabled) {
                return;
            }
            float thrust = getThrust (!Settings.engines_vac);
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

    [Obsolete("ModuleEnginesFX is now a subclass of ModuleEngines, so it shouldn't be needed... in theory.")]
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
        Dictionary<string, ModuleEngines> modes = new Dictionary<string, ModuleEngines> ();

        ModuleEngines activeMode {
            get { return modes[module.mode]; }
        }

        protected override ModuleEngines Engine {
            get { return activeMode; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        protected override void Init ()
        {
            module = GetComponent<MultiModeEngine> ();
            if (module == null) {
                throw new Exception ("Missing MultiModeEngine component.");
            }
            var engines = module.GetComponents<ModuleEngines> ();
            foreach (var eng in engines) {
                modes [eng.engineID] = eng;
            }
            GimbalRotation.addTo (gameObject);
        }
    }
}
