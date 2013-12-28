/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
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
using UnityEngine;

namespace RCSBuildAid
{
    public class DeltaV : MonoBehaviour
    {
        public static float dV = 0f;
        public static float burnTime = 0f;

        float thrust;
        float isp;
        float G = 9.81f;

        void Update ()
        {
            getIsp ();
            float fullMass = CoM_Marker.Mass;
            float resource;
            if (!DCoM_Marker.Resource.TryGetValue ("MonoPropellant", out resource)) {
                resource = 0;
            }
            float dryMass = fullMass - resource;
            dV = G * isp * Mathf.Log (fullMass / dryMass);
            burnTime = thrust < 0.001 ? 0 : resource * G * isp / thrust;
        }

        void getIsp ()
        {
            float dem = 0, num = 0;
            foreach (PartModule pm in RCSBuildAid.RCSlist) {
                RCSForce rcsF = pm.GetComponent<RCSForce> ();
                ModuleRCS rcs = (ModuleRCS)pm;
                float isp = rcs.atmosphereCurve.Evaluate (0f);
                foreach (VectorGraphic vector in rcsF.vectors) {
                    float thrust = vector.value.magnitude;
                    num += thrust * isp;
                    dem += thrust;
                }
            }
            if (dem == 0) {
                this.isp = 0;
                this.thrust = 0;
                return;
            }
            this.isp = num / dem;
            this.thrust = dem;
        }
    }
}

