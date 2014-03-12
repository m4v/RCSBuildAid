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
                print (String.Format ("TorqueGraphic: {0}", getCount (typeof(CircularVectorGraphic))));
                print (String.Format ("LineRenderer: {0}", getCount (typeof(LineRenderer))));

                print (String.Format ("Launch mass: {0}", CoM_Marker.Mass));
                print (String.Format ("Dry mass: {0}", DCoM_Marker.Mass));
                foreach (KeyValuePair<string, float> res in DCoM_Marker.Resource) {
                    print (String.Format("  {0}: {1}", res.Key, res.Value));
                }
            }
        }
    }
}

