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
            Vector3 distRotAxis = Vector3.Cross(distance, 
                                                RCSBuildAid.ReferenceVector.Torque.normalized);
            float mass = part.mass + part.GetResourceMass();
            value += mass * distRotAxis.sqrMagnitude;

            foreach (Part p in part.children) {
                recursePart (p);
            }
        }
    }
}

