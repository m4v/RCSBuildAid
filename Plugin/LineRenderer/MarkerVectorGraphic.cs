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
    public class MarkerVectorGraphic : VectorGraphic
    {
        public Vector3 valueTarget = Vector3.zero;

        LineRenderer target;

        protected override void Awake ()
        {
            base.Awake ();
            if (lines.Count == 2) {
                target = newLine ();
                lines.Add (target);
            } else {
                target = lines [2];
            }
        }

        protected override void Start ()
        {
            base.Start ();

            target.gameObject.layer = gameObject.layer;
            target.SetVertexCount (2);
            target.SetColors (color, color);

            offset = 0.6f;
            maxLength = 3f;
            minLength = 0.25f;
            maxWidth = 0.16f;
            minWidth = 0.05f;
            upperMagnitude = 5;
            lowerMagnitude = 0.05f;
        }

        protected override void calcDimentions (out float lenght, out float width)
        {
            lenght = calcDimentionLinear (minLength, maxLength);
            width = calcDimentionLinear (minWidth, maxWidth);
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();

            if (line.enabled) {
                /* target marker */
                if (valueTarget != Vector3.zero) {
                    target.SetWidth (0, width);
                    Vector3 p1 = startPoint + (valueTarget.normalized * lenght);
                    Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
                    target.SetPosition (0, p1);
                    target.SetPosition (1, p2);
                    target.enabled = true;
                } else {
                    target.enabled = false;
                }
            }
        }
    }
}
