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
using UnityEngine;

namespace RCSBuildAid
{
    public static class CelestialBodyExtensions
    {
        public static float ASLGravity (this CelestialBody body)
        {
            return (float)body.GeeASL * 9.81f;
        }

        public static float ASLPressure (this CelestialBody body)
        {
            /* body.atmodspherePressureSeaLevel seems to fail sometimes for some reason,
             * for Laythe is 0.6 atm but sometimes it would be 0.8 atm. fuck me */
//            return (float)body.atmospherePressureSeaLevel;
            return (float)body.GetPressure (0);
        }

        public static float ASLDensity (this CelestialBody body)
        {
            return (float)body.atmDensityASL;
        }

        public static float density (this CelestialBody body, float altitude)
        {
            return (float)body.GetDensity (body.GetPressure(altitude), 300);
        }

        public static float gravity (this CelestialBody body, float altitude)
        {
            return (float)body.gMagnitudeAtCenter / Mathf.Pow ((float)body.Radius + altitude, 2);
        }
    }
}

