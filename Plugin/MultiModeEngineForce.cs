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
        ModuleEngines primaryEngine;
        ModuleEngines secondaryEngine;
        VectorGraphic[] primaryVectors;
        VectorGraphic[] secondaryVectors;
        bool runningPrimary;

        ModuleEngines activeModule {
            get { return runningPrimary ? primaryEngine : secondaryEngine; }
        }

        protected override ModuleEngines Engine {
            get { return activeModule; }
        }

        protected override bool connectedToVessel {
            get { return RCSBuildAid.Engines.Contains (module); }
        }

        public override VectorGraphic[] vectors {
            get { return runningPrimary ? primaryVectors : secondaryVectors; }
        }

        protected override void Init ()
        {
            module = (MultiModeEngine)base.module;
            List<PartModule> engines = module.part.GetModulesOf<ModuleEngines> ();
            for (int i = 0; i < engines.Count; i++) {
                ModuleEngines eng = (ModuleEngines)engines [i];
                if (eng.engineID == module.primaryEngineID) {
                    primaryEngine = eng;
                } 
                if (eng.engineID == module.secondaryEngineID) {
                    secondaryEngine = eng;
                }
            }
            GimbalRotation.addTo (gameObject);
        }

        protected override void initVectors ()
        {
            color = Color.yellow;
            color.a = 0.75f;
            primaryVectors = getVectors (primaryEngine.thrustTransforms.Count);
            secondaryVectors = getVectors (secondaryEngine.thrustTransforms.Count);
        }

        protected override void destroyVectors ()
        {
            if (primaryVectors != null) {
                for (int i = 0; i < primaryVectors.Length; i++) {
                    if (primaryVectors [i] != null) {
                        Destroy (primaryVectors [i].gameObject);
                    }
                }
            }
            if (secondaryVectors != null) {
                for (int i = 0; i < secondaryVectors.Length; i++) {
                    if (secondaryVectors [i] != null) {
                        Destroy (secondaryVectors [i].gameObject);
                    }
                }
            }
        }

        protected override void Update ()
        {
            Debug.Assert (module != null, "[RCSBA, MultiModeEngineForce]: MultiModuleEngine is null");
            Debug.Assert (primaryEngine != null, "[RCSBA, MultiModeEngineForce]: primary ModuleEngine is null");
            Debug.Assert (secondaryEngine != null, "[RCSBA, MultiModeEngineForce]: secondary ModuleEngine is null");
            Debug.Assert (primaryVectors != null, "[RCSBA, MultiModeEngineForce]: primary vectors weren't initialized");
            Debug.Assert (secondaryVectors != null, "[RCSBA, MultiModeEngineForce]: secondary vectors weren't initialized");

            base.Update ();
            if (runningPrimary != module.runningPrimary) {
                runningPrimary = module.runningPrimary;
                /* changed mode, enable/disable the proper vectors */
                int i;
                for (i = primaryVectors.Length - 1; i >= 0; i--) {
                    primaryVectors [i].enabled = runningPrimary;
                }
                for (i = secondaryVectors.Length - 1; i >= 0; i--) {
                    secondaryVectors [i].enabled = !runningPrimary;
                }
            }
        }
    }
}
