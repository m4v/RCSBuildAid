/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
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
        public VectorGraphic[] vectors;

        int layer = 1;
        PartModule module;

        protected virtual void Awake (PartModule module)
        {
            this.module = module;
            gameObject.layer = layer;
            /* symmetry and clonning do this */
            if (vectors != null) {
                for (int i = 0; i < vectors.Length; i++) {
                    Destroy (vectors [i].gameObject);
                }
            }
        }

        protected virtual void Start ()
        {
            createVectors ();
        }

        protected virtual void Update ()
        {
            if (RCSBuildAid.Reference == null) {
                return;
            }

            if (!moduleList.Contains(module)) {
                Disable ();
                return;
            }

            /* the Editor clobbers the layer's value whenever you pick the part */
            if (gameObject.layer != layer) {
                gameObject.layer = layer;
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].layer = layer;
                }
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

        protected virtual void OnDestroy ()
        {
            for (int i = 0; i < vectors.Length; i++) {
                Destroy (vectors [i].gameObject);
            }
        }

        protected abstract List<PartModule> moduleList { get; }

        protected abstract void createVectors ();
    }

    /* Component for calculate and show forces in RCS */
    public class RCSForce : ModuleForces
    {
        float thrustPower;
        ModuleRCS module;

        protected override List<PartModule> moduleList {
            get { return RCSBuildAid.RCSlist; }
        }

        void Awake ()
        {
            module = GetComponent<ModuleRCS> ();
            if (module == null) {
                throw new Exception ("Missing ModuleRCS component.");
            }
            base.Awake (module);
        }

        protected override void createVectors ()
        {
            /* thrusterTransforms aren't initialized while in Awake, so in Start instead */
            GameObject obj;
            int n = module.thrusterTransforms.Count;
            vectors = new VectorGraphic[n];
            for (int i = 0; i < n; i++) {
                obj = new GameObject ("RCSVector");
                obj.layer = gameObject.layer;
                obj.transform.parent = transform;
                obj.transform.position = module.thrusterTransforms [i].position;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
            }
            thrustPower = module.thrusterPower;
        }

        protected override void Update ()
        {
            base.Update ();

            VectorGraphic vector;
            float magnitude;
            Vector3 thrustDirection;

            Vector3 normal = RCSBuildAid.Normal;
            if (RCSBuildAid.rcsMode == RCSMode.ROTATION) {
                normal = Vector3.Cross (transform.position - 
                                        RCSBuildAid.Reference.transform.position, normal);
            }

            /* calculate The Force  */
            for (int t = 0; t < module.thrusterTransforms.Count; t++) {
                thrustDirection = module.thrusterTransforms [t].up;
                magnitude = Mathf.Max (Vector3.Dot (thrustDirection, normal), 0f);
                magnitude = Mathf.Clamp (magnitude, 0f, 1f) * thrustPower;
                Vector3 vectorThrust = thrustDirection * magnitude;

                /* update VectorGraphic */
                vector = vectors [t];
                vector.value = vectorThrust;
                /* show it if there's force */
                vector.enabled = (magnitude > 0f) ? true : false;
            }
        }
    }

    public class EngineForce : ModuleForces
    {
        ModuleEngines module;

        protected override List<PartModule> moduleList {
            get { return RCSBuildAid.EngineList; }
        }

        void Awake ()
        {
            module = GetComponent<ModuleEngines> ();
            if (module == null) {
                throw new Exception ("Missing ModuleEngine component.");
            }
            base.Awake (module);
        }

        protected override void createVectors ()
        {
            GameObject obj;
            int n = module.thrustTransforms.Count;
            vectors = new VectorGraphic[n];
            for (int i = 0; i < n; i++) {
                obj = new GameObject ("EngineVector");
                obj.layer = gameObject.layer;
                obj.transform.parent = transform;
                obj.transform.position = module.thrustTransforms [i].position;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
                vectors [i].color = Color.yellow;
            }
        }

        protected override void Update ()
        {
            base.Update ();

            /* maxthrust = 1500 (mainsail) -> maxLength = 6 width = 0.3f
             * maxthrust = 1.5  (ant)      -> maxLength = 0.6 width = 0.03 */
            Func<float, float> calcLength = (t) => Mathf.Clamp (0.0036f * t + 0.6f, 0.6f, 6f);
            Func<float, float> calcWidth = (t) => calcLength (t) / 20f;

            int n = module.thrustTransforms.Count;
            float thrust = module.maxThrust / n;
            thrust *= module.thrustPercentage / 100;
            for (int i = 0; i < vectors.Length; i++) {
                if (module.part.inverseStage == RCSBuildAid.lastStage) {
                    /* RCS use the UP vector for direction of thrust, but no, engines use forward */
                    vectors [i].value = module.thrustTransforms [i].forward * thrust;
                    vectors [i].maxLength = calcLength (thrust);
                    vectors [i].width = calcWidth (thrust);
                } else {
                    vectors [i].value = Vector3.zero;
                }
            }
        }
    }
}
