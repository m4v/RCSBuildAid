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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public static class DebugSettings
    {
        public static bool inFlightAngularInfo = false;
        public static bool labelMagnitudes = false;
        public static bool startInOrbit = false;
    }

    /* 
     * this never was satisfactory, but I don't know how to measure these values in flight better 
     */

#if DEBUG
    [KSPAddon(KSPAddon.Startup.Flight, false)]
#endif
    [RequireComponent(typeof(GUIText))]
    public class InFlightReadings : MonoBehaviour
    {
        Vessel vessel;
        float time = 0;
        float longTime = 0;

        double oldVel = 0;
        double acc = 0;
        double maxAcc = 0;

        void Start ()
        {
            if (!DebugSettings.inFlightAngularInfo && !DebugSettings.startInOrbit) {
                gameObject.SetActive(false);
                return;
            }
            guiText.transform.position = new Vector3 (0.82f, 0.94f, 0f);
            vessel = FlightGlobals.ActiveVessel;
            guiText.text = "no vessel";
        }

        void FixedUpdate ()
        {
            if (vessel == null) {
                return;
            }
            if (DebugSettings.startInOrbit && !vessel.packed && vessel.Landed) {
                toOrbit ();
                if (!DebugSettings.inFlightAngularInfo) {
                    gameObject.SetActive(false);
                    return;
                }
            }
            double vel = vessel.angularVelocity.magnitude;
            time += TimeWarp.fixedDeltaTime;
            if (time > 0.1) {
                acc = (vel - oldVel) / time;
                maxAcc = Mathf.Max ((float)maxAcc, Mathf.Abs ((float)acc));
                oldVel = vel;
                time = 0;
            }
            longTime += TimeWarp.fixedDeltaTime;
            if (longTime > 10) {
                maxAcc = 0;
                longTime = 0;
            }
            Vector3 MOI = vessel.findLocalMOI (vessel.CoM);
            guiText.text = String.Format (
                "angvel: {0}\n" +
                "angmo: {1}\n" +
                "rotation: {11}\n" +
                "MOI: {2:F3} {3:F3} {4:F3}\n" + 
                "vel: {5:F5} rads {6:F5} degs\n" +
                "acc: {7:F5} rads {8:F5} degs\n" +
                "max: {9:F5} rads {10:F5} degs", 
                vessel.angularVelocity,
                vessel.angularMomentum,
                MOI.x, MOI.y, MOI.z,
                vel, toDeg (vel),
                acc, toDeg (acc),
                maxAcc, toDeg (maxAcc),
                vessel.transform.rotation);
        }

        double toDeg (double rad)
        {
            return rad * (180f / Math.PI);
        }

        void toOrbit ()
        {
            double altitude = 11461728000; /* 10000m/s orbital speed, convenient for verify dV readings */
            CelestialBody body = Planetarium.fetch.Sun;
            Vessel vessel = FlightGlobals.ActiveVessel;
            vessel.Landed = false;
            vessel.Splashed = false;
            vessel.landedAt = String.Empty;
            for (int i = vessel.Parts.Count -1; i >= 0; i--) {
                Part part = vessel.Parts[i];
                if (part.FindModulesImplementing<LaunchClamp> ().Count != 0) {
                    part.Die ();
                }
            }
            vessel.GoOnRails();
            Orbit orbit = new Orbit(0, 0, altitude + body.Radius, 0, 0, 0,
                                    Planetarium.GetUniversalTime(), body);
            vessel.orbitDriver.orbit = orbit;
            orbit.Init();
        }
    }

    /* Automaticaly load the game and go to the editor or active vessel */
#if DEBUG
    //[KSPAddon(KSPAddon.Startup.MainMenu, false)]
#endif
    public class AutoStart : MonoBehaviour
    {
        static bool done = false;

        public void Start ()
        {
            if (done) {
                return;
            }

            HighLogic.SaveFolder = "default";
            Game game = GamePersistence.LoadGame("quicksave", HighLogic.SaveFolder, true, false);
            game.startScene = GameScenes.EDITOR;
            //game.startScene = GameScenes.FLIGHT;
            game.Start();
            done = true;
        }
    }
}

