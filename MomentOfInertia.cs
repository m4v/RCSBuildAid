using System;
using UnityEngine;

namespace RCSBuildAid
{
    public class MomentOfInertia : MonoBehaviour
    {
        public float value;
        Vector3 axis;

        void Update ()
        {
            axis = RCSBuildAid.ReferenceVector.Torque.normalized;
            if (axis == Vector3.zero || EditorLogic.startPod == null) {
                /* no torque, calculating this is meaningless */
                return;
            }
            value = 0f;
            recursePart(EditorLogic.startPod);
        }

        void recursePart (Part part)
        {
            if (part.physicalSignificance ()) {
                /* Not sure if this moment of inertia matches the one vessels have in game */
                Vector3 distance = transform.position - (part.transform.position 
                    + part.transform.rotation * part.CoMOffset);
                Vector3 distAxis = Vector3.Cross (distance, axis);
                float mass = part.mass + part.GetResourceMassFixed ();
                value += mass * distAxis.sqrMagnitude;
            }

            foreach (Part p in part.children) {
                recursePart (p);
            }
        }
    }
}

