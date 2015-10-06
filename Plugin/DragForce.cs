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
using UnityEngine;

namespace RCSBuildAid
{
    public class DragForce : MonoBehaviour
    {
        VectorGraphic vector;
        public static Vector3 value;

        void Awake ()
        {
            var obj = new GameObject ("DragForce Vector object");
            obj.layer = gameObject.layer;
            obj.transform.parent = gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            vector = obj.AddComponent<VectorGraphic> ();
            vector.upperMagnitude = 150;
            vector.maxLength = 2;
            vector.maxWidth = 0.08f;
        }

        Vector3 flightDirection {
            get { return Vector3.up; }
        }

        void LateUpdate ()
        {
            float altitude = MenuParachutes.altitude;
            float mass = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ().mass;
            /* at terminal velocity drag force is same as gravity x mass */
            float force = Settings.selected_body.gravity (altitude) * mass;
            value = force * flightDirection;
            vector.value = value;
            vector.enabled = true;
        }

        float calculateDrag (float altitude, float speed, float mass)
        {
            float density = Settings.selected_body.density(altitude);
            return 0.5f * speed * speed * density * CoDMarker.drag_coef * PhysicsGlobals.DragMultiplier * mass;
        }
    }
}

