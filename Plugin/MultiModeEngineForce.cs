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
        [SerializeField]
        new MultiModeEngine module;
        ModuleEngines[] engineModules = new ModuleEngines[2];
        VectorGraphic[][] engineVectors = new VectorGraphic[2][];
        bool runningPrimary;

        ModuleEngines activeModule {
            get { return engineModules[runningPrimary ? 0 : 1]; }
        }

        protected override ModuleEngines Engine {
            get { return activeModule; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        public override VectorGraphic[] vectors {
            get { return engineVectors [runningPrimary ? 0 : 1]; }
        }

        protected override void Init ()
        {
            module = (MultiModeEngine)base.module;
            List<PartModule> engines = module.part.GetModulesOf<ModuleEngines> ();
            for (int i = 0; i < engines.Count; i++) {
                ModuleEngines eng = (ModuleEngines)engines [i];
                if (eng.engineID == module.primaryEngineID) {
                    engineModules [0] = eng;
                } else if (eng.engineID == module.secondaryEngineID) {
                    engineModules [1] = eng;
                }
            }
            GimbalRotation.addTo (gameObject);
        }

        protected override void initVectors ()
        {
            color = Color.yellow;
            color.a = 0.75f;
            engineVectors [0] = getVectors (engineModules [0].thrustTransforms.Count);
            engineVectors [1] = getVectors (engineModules [1].thrustTransforms.Count);
        }

        protected override void destroyVectors ()
        {
            for (int j = 0; j < 2; j++) {
                var v = engineVectors [j];
                if (v != null) {
                    for (int i = 0; i < v.Length; i++) {
                        if (v [i] != null) {
                            Destroy (v [i].gameObject);
                        }
                    }
                }
            }
        }

        protected override void Update ()
        {
            Debug.Assert (module != null, "[RCSBA, EngineForces]: ModuleEngines is null");
            Debug.Assert (thrustTransforms != null, "[RCSBA, EngineForces]: thrustTransform is null");
            Debug.Assert (vectors != null, "[RCSBA]: Vectors weren't initialized");
            Debug.Assert (vectors.Length == thrustTransforms.Count, 
                "[RCSBA]: Number of vectors doesn't match the number of transforms");

            base.Update ();
            if (!enabled) {
                return;
            }
            if (runningPrimary != module.runningPrimary) {
                runningPrimary = module.runningPrimary;
                /* changed mode, enable/disable the proper vectors */
                int i;
                for (i = engineVectors[0].Length - 1; i >= 0; i--) {
                    engineVectors[0] [i].enabled = runningPrimary;
                }
                for (i = engineVectors[1].Length - 1; i >= 0; i--) {
                    engineVectors[1] [i].enabled = !runningPrimary;
                }
            }
        }
    }
}
