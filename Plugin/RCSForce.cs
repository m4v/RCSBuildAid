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
                return RCSBuildAid.IncludeRCS;
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

        protected virtual float maxThrust {
            get { return module.thrusterPower; }
        }

        protected virtual float minThrust {
            get { return 0; }
        }

        protected virtual float vacIsp {
            get { return module.atmosphereCurve.Evaluate(0); }
        }

        protected virtual float getThrust ()
        {
            float p = module.thrustPercentage / 100;
            return Mathf.Lerp (minThrust, maxThrust, p);
        }

        protected Vector3 getDirection()
        {
            Vector3 vector = RCSBuildAid.TranslationVector;
            if (!module.enableX) {
                var n = RCSBuildAid.ReferenceTransform.right;
                vector -= Vector3.Dot (vector, n) * n;
            }
            if (!module.enableZ) {
                var n = RCSBuildAid.ReferenceTransform.up;
                vector -= Vector3.Dot (vector, n) * n;
            }
            if (!module.enableY) {
                var n = RCSBuildAid.ReferenceTransform.forward;
                vector -= Vector3.Dot (vector, n) * n;
            }
            return vector;
        }

        protected Vector3 getRotation() {
            Vector3 vector = RCSBuildAid.RotationVector;
            if (!module.enablePitch) {
                var n = RCSBuildAid.ReferenceTransform.right;
                vector -= Vector3.Dot(vector, n) * n;
            }
            if (!module.enableRoll) {
                var n = RCSBuildAid.ReferenceTransform.up;
                vector -= Vector3.Dot(vector, n) * n;
            }
            if (!module.enableYaw) {
                var n = RCSBuildAid.ReferenceTransform.forward;
                vector -= Vector3.Dot(vector, n) * n;
            }
            return vector;
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

            Vector3 directionVector = getDirection ();
            Vector3 rotationVector = getRotation ();

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
                    directionVector = Vector3.Cross (lever.normalized, rotationVector) * -1;
                }
                /* RCS usually use up as thrust direction */
                thrustDirection = module.useZaxis ? thrusterTransform.forward : thrusterTransform.up;
                magnitude = Mathf.Max (Vector3.Dot (thrustDirection, directionVector), 0f);
                if (module.fullThrust && (module.fullThrustMin <= magnitude)) {
                    magnitude = 1;
                }
                magnitude = Mathf.Clamp (magnitude, 0f, 1f) * getThrust();
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
}
