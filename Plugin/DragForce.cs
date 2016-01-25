/* Copyright © 2013-2016, Elián Hanisch <lambdae2@gmail.com>
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

        public Vector3 Vector {
            get { return vector.value; }
            set { vector.value = value; }
        }

        void Awake ()
        {
            var obj = new GameObject ("DragForce Vector object");
            obj.layer = gameObject.layer;
            obj.transform.parent = gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            vector = obj.AddComponent<VectorGraphic> ();
            vector.upperMagnitude = 150;
            vector.maxLength = 2;
            vector.minLength = 0.25f;
            vector.maxWidth = 0.08f;
            vector.minWidth = 0.02f;
            vector.offset = 0.6f;

            vector.value = Vector3.zero;
            vector.enabled = true;
        }
    }
}

