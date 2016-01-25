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

using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class MomentOfInertia : MonoBehaviour
    {
        public float value;
        Vector3 axis;

        void LateUpdate ()
        {
            if (!RCSBuildAid.Enabled) {
                return;
            }
            axis = RCSBuildAid.VesselForces.Torque().normalized;
            if (axis == Vector3.zero || EditorLogic.RootPart == null) {
                /* no torque, calculating this is meaningless */
                return;
            }
            value = 0f;

            EditorUtils.RunOnAllParts (calculateMoI);
        }

        void calculateMoI (Part part)
        {
            if (part.GroundParts ()) {
                return;
            }

            Vector3 com;
            if (part.GetCoM (out com)) {
                /* Not sure if this moment of inertia matches the one vessels have in game */
                Vector3 distance = transform.position - com;
                Vector3 distAxis = Vector3.Cross (distance, axis);
                value += part.GetTotalMass() * distAxis.sqrMagnitude;
            }
        }
    }
}

