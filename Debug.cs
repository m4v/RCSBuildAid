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
}

