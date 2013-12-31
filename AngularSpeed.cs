using System;
using UnityEngine;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    [RequireComponent(typeof(GUIText))]
    public class AngularSpeedDebug : MonoBehaviour
    {
        Vessel vessel;
        Rigidbody root;

        double oldVel = 0;

        void Start ()
        {
            guiText.transform.position = new Vector3 (0.82f, 0.94f, 0f);
            vessel = FlightGlobals.ActiveVessel;
            guiText.text = "no rigidbody";
        }

        void FixedUpdate ()
        {
            root = vessel.rootPart.GetComponent<Rigidbody> ();
            if (root == null) {
                return;
            }
            double vel = root.angularVelocity.magnitude;
            double acc = (vel - oldVel) / TimeWarp.fixedDeltaTime;
            acc = Mathf.Abs((float)acc);
            oldVel = vel;
            guiText.text = String.Format ("{0}\n{1:F5} rads {2:F5} degs\n" +
                                          "{3:F5} rads {4:F5} degs", 
                                          root.angularVelocity,
                                          vel, toDeg(vel), 
                                          acc, toDeg (acc));
        }

        double toDeg (double rad)
        {
            return rad * (180f / Math.PI);
        }

    }
}

