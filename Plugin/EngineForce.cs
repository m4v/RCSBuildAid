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
    /* Component for calculate and show forces in engines */
    public class EngineForce : ModuleForces
    {
        ModuleEngines module;

        static DictionaryValueList<PartModule, ModuleForces> ModuleDict = new DictionaryValueList<PartModule, ModuleForces> ();

        public static List<ModuleForces> List = new List<ModuleForces>();

        public static void Add(PartModule mod)
        {
            if (ModuleDict.ContainsKey(mod)) {
                return;
            }

            EngineForce mf = mod.gameObject.AddComponent<EngineForce> ();
            mf.module = (ModuleEngines)mod;
            ModuleDict [mod] = mf;
            List.Add (mf);
            #if DEBUG
            Debug.Log (String.Format ("[RCSBA]: Adding EngineForce for {0}, total count {1}",
                mod.part.partInfo.name, ModuleDict.Count));
            #endif
        }

        protected override void Cleanup()
        {
            #if DEBUG
            Debug.Log ("[RCSBA]: EngineForce cleanup");
            #endif
            List.Remove (this);
            if (module != null) {
                ModuleDict.Remove (module);
            }
        }

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

        protected override void initVectors ()
        {
            color = Color.yellow;
            color.a = 0.75f;
            base.initVectors ();
        }

        protected override void configVector (VectorGraphic vector)
        {
            base.configVector (vector);
            vector.upperMagnitude = 1500f;
            vector.lowerMagnitude = 0.025f;
            vector.maxLength = 4f;
            vector.minLength = 0.5f;
            vector.maxWidth = 0.2f;
            vector.minWidth = 0.04f;
        }

        protected override void Init ()
        {
            GimbalRotation.addTo (gameObject);
        }

        protected override void Update ()
        {
            base.Update ();
            if (!enabled) {
                return;
            }
            float thrust = getThrust (!Settings.engines_vac);
            for (int i = vectors.Length - 1; i >= 0; i--) {
                if (Part.inverseStage == RCSBuildAid.LastStage) {
                    Transform t = thrustTransforms [i];
                    /* engines use forward as thrust direction */
                    vectors [i].value = t.forward * thrust;
                } else {
                    vectors [i].value = Vector3.zero;
                }
            }
        }
    }
}
