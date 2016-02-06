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
    public class CircularVectorGraphic : ArrowBase
    {
        public float minRadius = 0.6f;
        public float maxRadius = 3f;
        public float maxWidth = 0.16f;
        public float minWidth = 0.02f;
        public int vertexCount = 48;

        protected override void Start ()
        {
            upperMagnitude = 1;
            lowerMagnitude = 0.01f;

            Color circleColor = Color.red;
            circleColor.a = 0.5f;
            line.SetVertexCount(vertexCount - 3);
            line.useWorldSpace = false;
            line.SetColors(circleColor, circleColor);

            lineEnd.SetVertexCount(2);
            lineEnd.useWorldSpace = false;
            lineEnd.SetColors(circleColor, circleColor);

            lineEnd.gameObject.layer = gameObject.layer;
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();

            if (line.enabled) {
                /* calc width */
                float width = calcDimentionExp(minWidth, maxWidth);
                setWidth (width);

                /* calc radius */
                float radius = calcDimentionExp(minRadius, maxRadius);

                /* Draw our circle */
                float angle = 2 * Mathf.PI / vertexCount;
                const float pha = Mathf.PI * 4f / 9f; /* phase angle, so the circle starts and ends at the
                                                 translation vector */
                Func<float, float, float> calcx = (a, r) => r * Mathf.Cos( a - pha);
                Func<float, float, float> calcy = (a, r) => r * Mathf.Sin(-a + pha);
                float x, y, z = 0;
                Vector3 v = Vector3.zero;
                int i = 0;
                for (; i < vertexCount - 3; i++) {
                    x = calcx(angle * i, radius);
                    y = calcy(angle * i, radius);
                    v = new Vector3(x, y, z);
                    line.SetPosition(i, v);
                }

                /* Finish with arrow */
                lineEnd.SetPosition(0, v);
                /* do the math for get the arrow tip tangent to the circle, we do this so
                 * it doesn't look too broken */
                float radius2 = radius / Mathf.Cos(angle * 2);
                lineEnd.SetPosition(1, new Vector3(calcx (angle * (i + 1), radius2),
                                                 calcy (angle * (i + 1), radius2),
                                                 z));
            }
        }
    }
}
