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
    public partial class RCSBuildAid : MonoBehaviour
    {
        /*
         * Debug stuff
         */

        internal Stopwatch SW = new Stopwatch ();
        internal float counter = 0;

        [Conditional("DEBUG")]
        void debugStartTimer ()
        {
            if (guiText == null) {
                gameObject.AddComponent<GUIText> ();
                guiText.transform.position = new Vector3 (0.93f, 0.92f, 0f);
                guiText.text = "time:";
            }
            SW.Start();
        }

        [Conditional("DEBUG")]
        void debugStopTimer ()
        {
            SW.Stop ();
            counter++;
            if (counter > 200) {
                float callTime = SW.ElapsedMilliseconds / counter;
                counter = 0;
                SW.Reset();
                guiText.text = String.Format("time {0:F2}", callTime);
            }
        }

        [Conditional("DEBUG")]
        void debugPrint ()
        {
            if (Input.GetKeyDown (KeyCode.Space)) {
                Func<Type, int> getCount = (type) => GameObject.FindObjectsOfType (type).Length;
                print (String.Format ("ModuleRCS: {0}", getCount (typeof(ModuleRCS))));
                print (String.Format ("ModuleEngines: {0}", getCount (typeof(ModuleEngines))));
                print (String.Format ("RCSForce: {0}", getCount (typeof(RCSForce))));
                print (String.Format ("EngineForce: {0}", getCount (typeof(EngineForce))));
                print (String.Format ("VectorGraphic: {0}", getCount (typeof(VectorGraphic))));
                print (String.Format ("TorqueGraphic: {0}", getCount (typeof(TorqueGraphic))));
                print (String.Format ("LineRenderer: {0}", getCount (typeof(LineRenderer))));

                print (String.Format ("Launch mass: {0}", CoM_Marker.Mass));
                print (String.Format ("Dry mass: {0}", DCoM_Marker.Mass));
                foreach (KeyValuePair<string, float> res in DCoM_Marker.Resource) {
                    print (String.Format("  {0}: {1}", res.Key, res.Value));
                }
            }
        }
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
            guiText.transform.position = new Vector3 (0.82f, 0.94f, 0f);
            vessel = FlightGlobals.ActiveVessel;
            guiText.text = "no vessel";
        }

        void FixedUpdate ()
        {
            if (vessel == null) {
                return;
            }
            double vel = vessel.angularVelocity.magnitude;
            time += TimeWarp.fixedDeltaTime;
            if (time > 0.1) {
                acc = (vel - oldVel) / time;
                maxAcc = Mathf.Max((float)maxAcc, Mathf.Abs((float)acc));
                oldVel = vel;
                time = 0;
            }
            longTime += TimeWarp.fixedDeltaTime;
            if (longTime > 10) {
                maxAcc = 0;
                longTime = 0;
            }
            Vector3 MOI = vessel.findLocalMOI(vessel.CoM);
            guiText.text = String.Format ("angvel: {0}\n" +
                                          "angmo: {1}\n" +
                                          "MOI: {2:F3} {3:F3} {4:F3}\n" + 
                                          "vel: {5:F5} rads {6:F5} degs\n" +
                                          "acc: {7:F5} rads {8:F5} degs\n" +
                                          "max: {9:F5} rads {10:F5} degs",
                                          vessel.angularVelocity,
                                          vessel.angularMomentum,
                                          MOI.x, MOI.y, MOI.z,
                                          vel, toDeg(vel), 
                                          acc, toDeg (acc),
                                          maxAcc, toDeg(maxAcc));
        }

        double toDeg (double rad)
        {
            return rad * (180f / Math.PI);
        }
    }
}

