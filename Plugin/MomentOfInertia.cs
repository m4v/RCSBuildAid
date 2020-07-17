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
    public class MomentOfInertia : MonoBehaviour
    {
        public float value;
        Vector3 axis;

        void LateUpdate ()
        {
            Profiler.BeginSample("[RCSBA] MoI LateUpdate"); 
            if (!RCSBuildAid.Enabled) {
                Profiler.EndSample();
                return;
            }
            axis = RCSBuildAid.VesselForces.Torque().normalized;
            if (axis == Vector3.zero || EditorLogic.RootPart == null) {
                /* no torque, calculating this is meaningless */
                Profiler.EndSample();
                return;
            }
            value = 0f;

            EditorUtils.RunOnVesselParts (calculateMoI);
            EditorUtils.RunOnSelectedParts (calculateMoI);
            Profiler.EndSample();
        }

        void calculateMoI (Part part)
        {
            Profiler.BeginSample("[RCSBA] MoI calculateMoI");
            if (part.GroundParts ()) {
                Profiler.EndSample();
                return;
            }

            Vector3 com = part.GetCoM();
            /* Not sure if this moment of inertia matches the one vessels have in flight */
            Vector3 distance = transform.position - com;
            Vector3 distAxis = Vector3.Cross (distance, axis);
            value += part.GetTotalMass() * distAxis.sqrMagnitude;
            Profiler.EndSample();
        }
    }
}

