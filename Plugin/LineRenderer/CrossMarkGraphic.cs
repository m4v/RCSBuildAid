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
    public class CrossMarkGraphic : LineBase
    {
        const float scale = 1.2f;

        protected override void Awake ()
        {
            base.Awake ();
            for (int i = 0; i < 6; i++) {
                lines.Add (newLine ());
            }
            foreach (var lineRenderer in lines) {
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetVertexCount (2);
                lineRenderer.SetPosition (0, Vector3.zero);
            }

            lines[0].SetPosition (1, Vector3.forward * scale);
            lines[1].SetPosition (1, Vector3.forward * -scale);
            lines[2].SetPosition (1, Vector3.right * scale);
            lines[3].SetPosition (1, Vector3.right * -scale);
            lines[4].SetPosition (1, Vector3.up * scale);
            lines[5].SetPosition (1, Vector3.up * -scale);
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();
            setWidth (0, 0.05f * transform.localScale.magnitude);
        }
    }
}
