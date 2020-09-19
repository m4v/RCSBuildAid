/* Copyright © 2013-2020, Elián Hanisch <lambdae2@gmail.com>
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

namespace RCSBuildAid
{
    public class ArrowBase : LineBase
    {
        public float maximumMagnitude = 4f; /* vector size caps at this magnitude */
        public float minimumMagnitude;      /* lower than this and the vector won't be displayed */
        public float maxLength = 1.5f;
        public float minLength = 0.1f;
        public float maxWidth = 0.05f;
        public float minWidth = 0.02f;
        
        [SerializeField]
        protected LineRenderer line;
        [SerializeField]
        protected LineRenderer lineEnd;

        Vector3 internalValue = Vector3.zero;

        static AnimationCurve lengthCurve = new AnimationCurve(new[]
        {
            new Keyframe(0, 0, 0, 3),
            new Keyframe(1, 1, 0, 0)
        });
        static AnimationCurve widthCurve = new AnimationCurve(new[]
        {
            new Keyframe(0, 0, 0, 1),
            new Keyframe(1, 1, 0, 0)
        });

        public new virtual bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                bool v = value && internalValue.magnitude > minimumMagnitude;
                line.enabled = v;
                lineEnd.enabled = v;
            }
        }

        public Vector3 value {
            get { return internalValue; }
            set {
                internalValue = value;
                bool v = enabled && value.magnitude > minimumMagnitude;
                line.enabled = v;
                lineEnd.enabled = v;
            }
        }

        public override void setWidth (float v2)
        {
            line.startWidth = v2;
            line.endWidth = v2;
            lineEnd.startWidth = v2 * 3;
            lineEnd.endWidth = 0;
        }
        
        protected virtual void calcDimensions (out float length, out float width)
        {
            var normalizedMagnitude = value.magnitude / maximumMagnitude;
            length = lengthCurve.Evaluate(normalizedMagnitude) * (maxLength - minLength) + minLength;
            width = widthCurve.Evaluate(normalizedMagnitude) * (maxWidth - minWidth) + minWidth;
        }

        protected override void Awake ()
        {
#if DEBUG
            Debug.Log("[RCSBA, ArrowBase]: Awake");
#endif
            base.Awake ();
            line = newLine();
            /* arrow point */
            lineEnd = newLine();
        }
        
        void OnDestroy()
        {
#if DEBUG
            Debug.Log("[RCSBA, ArrowBase]: OnDestroy");
#endif
        }
    }
}
