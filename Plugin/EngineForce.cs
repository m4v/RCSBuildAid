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
                    /* engines use forward as thrust direction */
                    vectors [i].value = t.forward * thrust;
                } else {
                    vectors [i].value = Vector3.zero;
                }
            }
        }
    }
}
