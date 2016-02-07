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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    [RequireComponent(typeof(LineRenderer))]
    public class ArrowBase : LineBase
    {
        /* magnitude limits for the graphical representation */
        public float upperMagnitude = 4;
        public float lowerMagnitude = 0.01f;

        public Vector3 value = Vector3.zero;

        protected LineRenderer line;
        protected LineRenderer lineEnd;

        bool holdUpdate = true;

        public override bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                if (!holdUpdate || !value) {
                    enableLines (value);
                }
            }
        }

        public override void setWidth (float v2)
        {
            line.SetWidth (v2, v2);
            lineEnd.SetWidth (v2 * 3, 0);
        }

        protected override void Awake ()
        {
            base.Awake ();
            if (lines.Count == 0) {
                line = GetComponent<LineRenderer> ();
                line.material = material;
                lines.Add (line);

                /* arrow point */
                lineEnd = newLine ();
                lines.Add (lineEnd);
            } else {
                line = lines [0];
                lineEnd = lines [1];
            }

            Events.ModeChanged += onModeChange;
        }

        void OnDestroy ()
        {
            Events.ModeChanged -= onModeChange;
        }

        protected float calcDimentionExp (float miny, float maxy)
        {
            /* exponential scaling makes changes near zero more noticeable */
            float T = 5 / upperMagnitude;
            float A = (maxy - miny) / Mathf.Exp(-lowerMagnitude * T);
            float v = Mathf.Clamp(value.magnitude, lowerMagnitude, upperMagnitude);
            return maxy - A * Mathf.Exp(-v * T);
        }

        protected float calcDimentionLinear (float miny, float maxy)
        {
            float m = (maxy - miny) / (upperMagnitude - lowerMagnitude);
            float b = maxy - upperMagnitude * m;
            float v = Mathf.Clamp(value.magnitude, lowerMagnitude, upperMagnitude);
            return v * m + b;
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();
            enableLines (!holdUpdate && (value.magnitude >= lowerMagnitude));
            holdUpdate = false;
        }

        void onModeChange (PluginMode mode)
        {
            holdUpdate = true;
        }
    }
}
