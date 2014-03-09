using System;
using UnityEngine;

namespace RCSBuildAid
{
    public class MomentOfInertia : MonoBehaviour
    {
        public float value;

        void LateUpdate ()
        {
            value = 0f;
            recursePart(EditorLogic.startPod);
        }

        void recursePart (Part part)
        {
            /* Not sure if this moment of inertia matches the one vessels have in game */
            Vector3 distance = transform.position - (part.transform.position 
                + part.transform.rotation * part.CoMOffset);
            Vector3 axis = RCSBuildAid.ReferenceVector.Torque.normalized;
            if (axis == Vector3.zero) {
                axis = Vector3.up; /* there's no torque, any vector will do */
            }
            Vector3 distAxis = Vector3.Cross(distance, axis);
            float mass = part.mass + part.GetResourceMass();
            value += mass * distAxis.sqrMagnitude;

            foreach (Part p in part.children) {
                recursePart (p);
            }
        }
    }
}

