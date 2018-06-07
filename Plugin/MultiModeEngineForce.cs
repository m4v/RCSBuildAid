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
    /* Component for calculate and show forces in engines such as RAPIER */
    public class MultiModeEngineForce : EngineForce
    {
        MultiModeEngine module;
        Dictionary<string, ModuleEngines> engineModules = new Dictionary<string, ModuleEngines> ();
        Dictionary<string, VectorGraphic[]> engineVectors = new Dictionary<string, VectorGraphic[]> ();
        int modeHash;

        ModuleEngines activeModule {
            get { return engineModules[module.mode]; }
        }

        protected override ModuleEngines Engine {
            get { return activeModule; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        public override VectorGraphic[] vectors {
            get { return engineVectors [module.mode]; }
        }

        protected override void Init ()
        {
            module = GetComponent<MultiModeEngine> ();
            if (module == null) {
                throw new Exception ("Missing MultiModeEngine component.");
            }
            var engines = module.GetComponents<ModuleEngines> ();
            foreach (var eng in engines) {
                engineModules [eng.engineID] = eng;
            }
            GimbalRotation.addTo (gameObject);
        }

        protected override void initVectors ()
        {
            color = Color.yellow;
            color.a = 0.75f;
            foreach (var eng in engineModules.Values) {
                engineVectors[eng.engineID] = getVectors (eng.thrustTransforms.Count);
            }
        }

        protected override void destroyVectors ()
        {
            var list = engineVectors.Values;
            foreach (var v in list) {
                for (int i = 0; i < v.Length; i++) {
                    if (v [i] != null) {
                        Destroy (v [i].gameObject);
                    }
                }
            }
            engineVectors.Clear ();
        }

        protected override void Update ()
        {
            base.Update ();
            var mode = module.mode.GetHashCode ();
            if (modeHash != mode) {
                modeHash = mode;
                /* changed mode, enable/disable the proper vectors */
                foreach (var pair in engineVectors) {
                    var vg = pair.Value;
                    var value = mode == pair.Key.GetHashCode ();
                    for (int i = 0; i < vg.Length; i++) {
                        vg [i].enabled = value;
                    }
                }
            }
        }
    }
}
