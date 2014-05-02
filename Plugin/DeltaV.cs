/* Copyright © 2013-2014, Elián Hanisch <lambdae2@gmail.com>
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
using UnityEngine;

namespace RCSBuildAid
{
    public class DeltaV : MonoBehaviour
    {
        public static float dV = 0f;
        public static float burnTime = 0f;
        public static bool sanity;

        float isp;
        const float G = 9.81f; /* by isp definition */

        void Update ()
        {
            sanity = true;
            float resource = 0;
            switch (RCSBuildAid.mode) {
            case PluginMode.RCS:
                resource = getResourceMass();
                break;
            case PluginMode.Engine:
            default:
                dV = 0;
                burnTime = 0;
                return;
            }
            calcIsp ();
            float fullMass = CoM_Marker.Mass;
            float dryMass = fullMass - resource;
            dV = G * isp * Mathf.Log (fullMass / dryMass);

            float thrust = RCSBuildAid.VesselForces.Thrust().magnitude;
            burnTime = thrust < 0.001 ? 0 : resource * G * isp / thrust;
#if DEBUG
            if (Input.GetKeyDown(KeyCode.Space)) {
                print (String.Format ("delta v: {0}", dV));
                print (String.Format ("full mass: {0} dry mass: {1} resource: {2}", 
                                      fullMass, dryMass, resource));
                print (String.Format ("isp: {0} thrust: {1}", isp, thrust));
            }
#endif
        }

        float getResourceMass ()
        {
            float resourceMass = 0;
            HashSet<string> counted = new HashSet<string> ();
            foreach (PartModule pm in RCSBuildAid.RCSlist) {
                ModuleRCS rcs = (ModuleRCS)pm;
                if (!counted.Contains (rcs.resourceName)) {
                    float res = 0;
                    DCoMResource dcomRes;
                    if (DCoM_Marker.Resource.TryGetValue (rcs.resourceName, out dcomRes)) {
                        res = (float)dcomRes.mass;
                    }
                    resourceMass += res;
                    counted.Add(rcs.resourceName);
                    PartResourceDefinition resInfo = 
                        PartResourceLibrary.Instance.GetDefinition(rcs.resourceName);
                    switch(resInfo.resourceFlowMode) {
                    case ResourceFlowMode.ALL_VESSEL:
                    case ResourceFlowMode.STAGE_PRIORITY_FLOW:
                        break;
                    default:
                        sanity = false;
                        break;
                    }
                }
            }
            return resourceMass;
        }

        void calcIsp ()
        {
            float denominator = 0, numerator = 0;
            switch (RCSBuildAid.mode) {
            case PluginMode.RCS:
                calcRCSIsp (ref numerator, ref denominator);
                break;
            case PluginMode.Engine:
            default:
                isp = 0;
                return;
            }
            if (denominator == 0) {
                isp = 0;
                return;
            }
           isp = numerator / denominator; /* weighted mean */
        }

        void calcRCSIsp (ref float num, ref float den)
        {
            foreach (PartModule pm in RCSBuildAid.RCSlist) {
                ModuleForces forces = pm.GetComponent<ModuleForces> ();
                if (forces && forces.enabled) {
                    ModuleRCS mod = (ModuleRCS)pm;
                    float isp = mod.atmosphereCurve.Evaluate (0f);
                    foreach (VectorGraphic vector in forces.vectors) {
                        Vector3 thrust = vector.value;
                        float isp2 = Vector3.Dot (isp * thrust.normalized, 
                                 RCSBuildAid.VesselForces.Thrust().normalized * -1);
                        /* calculating weigthed mean, RCS thrust magnitude is already "weigthed" */
                        num += thrust.magnitude * isp2;
                        den += thrust.magnitude;
                    }
                }
            }
        }
    }
}

