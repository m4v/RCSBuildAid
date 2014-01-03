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
        float time = 0f;

        double oldVel = 0f;
        double acc = 0f;

        void Start ()
        {
            guiText.transform.position = new Vector3 (0.82f, 0.94f, 0f);
            vessel = FlightGlobals.ActiveVessel;
            guiText.text = "no rigidbody";
        }

        void FixedUpdate ()
        {
            root = vessel.rootPart.rb;
            if (root == null) {
                return;
            }
            time += TimeWarp.fixedDeltaTime;
            double vel = vessel.angularVelocity.magnitude;
            if (time > 0.1f) {
                acc = (vel - oldVel) / time;
                oldVel = vel;
                time = 0f;
            }
            Vector3 MOI = vessel.findLocalMOI(vessel.CoM);
            guiText.text = String.Format ("root: {0}\n" +
                                          "vessel angvel: {5}\n" +
                                          "vessel angmo: {6}\n" +
                                          "{1:F5} rads {2:F5} degs\n" +
                                          "{3:F5} rads {4:F5} degs\n" +
                                          "MOI: {7:F3} {8:F3} {9:F3}", 
                                          root.angularVelocity,
                                          vel, toDeg(vel), 
                                          acc, toDeg (acc),
                                          vessel.angularVelocity,
                                          vessel.angularMomentum,
                                          MOI.x, MOI.y, MOI.z);
        }

        double toDeg (double rad)
        {
            return rad * (180f / Math.PI);
        }

    }

    public class AngularMass : MonoBehaviour
    {
        public float value;

        void LateUpdate ()
        {
            value = 0f;
            recursePart(EditorLogic.startPod);
        }

        void recursePart (Part part)
        {
            Vector3 distance = transform.position - (part.transform.position 
                + part.transform.rotation * part.CoMOffset);
            Vector3 distRotAxis = Vector3.Cross(distance, RCSBuildAid.Normal);
            float mass = part.mass + part.GetResourceMass();
            value += mass * distRotAxis.sqrMagnitude;

            foreach (Part p in part.children) {
                recursePart (p);
            }
        }
    }
}

