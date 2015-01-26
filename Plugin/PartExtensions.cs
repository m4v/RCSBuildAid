/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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
    public static class PartExtensions
    {
        // TODO move this stuff into settings.cfg
        static HashSet<string> nonPhysicsParts = new HashSet<string> {
            "launchClamp1", /* has mass at launch, but accounting it is worthless */
        };

        public static bool hasPhysicsEnabled (this Part part)
        {
            if (part == EditorLogic.RootPart) {
                return true;
            }
            if (part.PhysicsSignificance == (int)Part.PhysicalSignificance.NONE) {
                return false;
            }
            if (part.physicalSignificance == Part.PhysicalSignificance.NONE) {
                return false;
            }
            if (nonPhysicsParts.Contains (part.partInfo.name)) {
                return false;
            }
            return true;
        }

        public static float GetResourceMassFixed (this Part part) {
            float mass = part.GetResourceMass();
            /* with some outdated mods, it can return NaN */
            return float.IsNaN (mass) ? 0 : mass;
        }

        public static float GetTotalMass (this Part part) {
            return part.GetResourceMassFixed() + part.mass;
        }

        public static float GetSelectedMass (this Part part) {
            float mass = part.mass;
            for (int i = 0; i < part.Resources.Count; i++) {
                PartResource res = part.Resources [i];
                // Analysis disable once CompareOfFloatsByEqualityOperator
                if (res.info.density == 0) {
                    continue;
                }
                if (Settings.GetResourceCfg (res.info.name, false)) {
                    mass += (float)(res.amount * res.info.density);
                }
            }
            return mass;
        }
    }

    public static class CelestialBodyExtensions
    {
        public static float density (this CelestialBody body, float altitude)
        {
            double pressure = FlightGlobals.getStaticPressure (altitude, body);
            return (float)FlightGlobals.getAtmDensity (pressure);
        }

        public static float gravity (this CelestialBody body, float altitude)
        {
            return (float)body.gMagnitudeAtCenter / Mathf.Pow ((float)body.Radius + altitude, 2);
        }
    }
}

