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
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Profiling;

namespace RCSBuildAid
{
    public class VectorGraphic : ArrowBase
    {
        public float offset;

        protected float length;
        protected float width;

        [SerializeField]
        GUIText debugLabel;

        protected override void enableLines (bool value)
        {
            base.enableLines (value);
            enableDebugLabel (value);
        }

        [Conditional("DEBUG")]
        void enableDebugLabel (bool v) {
            if (debugLabel != null) {
                debugLabel.enabled = v;
            }
        }


        protected override void Awake ()
        {
            base.Awake ();
            line.positionCount = 2;
            lineEnd.positionCount = 2;
        }

        protected override void LateUpdate ()
        {
            Profiler.BeginSample("[RCSBA] VectorGraphic LateUpdate");
            base.LateUpdate ();

            if (line.enabled) {
                /* calc dimensions */
                calcDimensions (out length, out width);
                setWidth (width);

                Vector3 norm = value.normalized;
                Vector3 startPoint = transform.position + norm * offset;
                Vector3 vector = length * norm;
                Vector3 endPoint = startPoint + vector;

                /* calculate arrow tip length */
                float arrowL = Mathf.Clamp (vector.magnitude / 2f, 0f, width * 4);
                Vector3 midPoint = endPoint - vector.normalized * arrowL;

                line.SetPosition (0, startPoint);
                line.SetPosition (1, midPoint);

                lineEnd.SetPosition (0, midPoint);
                lineEnd.SetPosition (1, endPoint);

                showDebugLabel (endPoint);
            }
            Profiler.EndSample();
        }

        [Conditional("DEBUG")]
        void showDebugLabel (Vector3 endPoint)
        {
            if (DebugSettings.labelMagnitudes) {
                if (debugLabel == null) {
                    var obj = new GameObject ("VectorGraphic debug label");
                    obj.transform.parent = transform;
                    debugLabel = obj.AddComponent<GUIText> ();
                }
                debugLabel.enabled = true;
                debugLabel.transform.position = 
                    EditorLogic.fetch.editorCamera.WorldToViewportPoint (endPoint);
                if (value.magnitude > 0f) {
//                    Vector3 lever = RCSBuildAid.ReferenceMarker.transform.position - transform.position;
//                    float angle = Vector3.Angle(lever, value) * Mathf.Deg2Rad;
//                    debugLabel.text = String.Format ("force: {0:0.##}\nlever: {1:0.##}\nsin: {2:0.##}", 
//                                                     value.magnitude, lever.magnitude, Mathf.Sin (angle));
                    debugLabel.text = string.Format(value.magnitude.ToString("0.##"));
                } else {
                    debugLabel.text = string.Empty;
                }
            } else {
                if (debugLabel != null) {
                    debugLabel.enabled = false;
                }
            }
        }
    }
}
