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

using UnityEngine;
using UnityEngine.Profiling;

namespace RCSBuildAid
{
    public class MarkerVectorGraphic : VectorGraphic
    {
        [SerializeField]
        LineRenderer target;
        Vector3 internalValueTarget = Vector3.zero;
        
        public Vector3 valueTarget {
            get { return internalValueTarget; }
            set {
                target.enabled = enabled && value != Vector3.zero;
                internalValueTarget = value;
            }
        }

        public override bool enabled {
            get { return base.enabled; }
            set {
                target.enabled = value && valueTarget != Vector3.zero;
                base.enabled = value;
            }
        }

        protected override void Awake ()
        {
            base.Awake ();
            target = newLine ();
        }

        protected override void Start ()
        {
            base.Start ();
            target.positionCount = 2;
            target.enabled = false;
            
            offset = 0.6f;
            maxLength = 3f;
            minLength = 0.25f;
            maxWidth = 0.16f;
            minWidth = 0.05f;
            maximumMagnitude = 5;
            minimumMagnitude = 0.05f;
        }

        protected override void LateUpdate ()
        {
            Profiler.BeginSample("[RCSBA] MarkerVectorGraphic LateUpdate");
            base.LateUpdate ();

            if (target.enabled) {
                /* target marker */
                target.startWidth = 0;
                target.endWidth = width;
                Vector3 p1 = transform.position + valueTarget.normalized * (length + offset);
                Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
                target.SetPosition (0, p1);
                target.SetPosition (1, p2);
            }
            Profiler.EndSample();
        }
    }
}
