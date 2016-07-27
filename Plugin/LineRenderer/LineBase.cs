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
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class LineBase : MonoBehaviour
    {
        //string shader = "GUI/Text Shader"; /* solid and on top of everything in that layer */
        public const string shader = "Particles/Alpha Blended"; /* solid */
        //public static string shader = "Particles/Additive";

        /* Need SerializeField or clonning will fail to pick these private variables */
        [SerializeField]
        protected Color color = Color.cyan;
        [SerializeField]
        protected List<LineRenderer> lines = new List<LineRenderer> ();
        protected Material material;

        const int layer = 2;

        public virtual void setColor (Color value) {
            color = value;
            foreach(var line in lines) {
                line.SetColors (value, value);
            }
        }

        public virtual void setWidth(float v1, float v2) {
            foreach(var line in lines) {
                line.SetWidth (v1, v2);
            }
        }

        public virtual void setWidth (float v2)
        {
            setWidth (v2, v2);
        }

        public new virtual bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                enableLines (value);
            }
        }

        protected virtual void enableLines (bool value)
        {
            foreach(var line in lines) {
                line.enabled = value;
            }
        }

        protected LineRenderer newLine ()
        {
            var obj = new GameObject("RCSBuildAid LineRenderer object");
            var lr = obj.AddComponent<LineRenderer>();
            obj.transform.parent = gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            lr.material = material;
            return lr;
        }

        protected virtual void Awake ()
        {
            material = new Material (Shader.Find (shader));
        }

        protected virtual void Start ()
        {
            setColor (color);
            setLayer ();
        }

        protected virtual void LateUpdate ()
        {
            checkLayer ();
        }

        void checkLayer ()
        {
            /* the Editor clobbers the layer's value whenever you pick the part */
            if (gameObject.layer != layer) {
                setLayer ();
            }
        }

        public virtual void setLayer ()
        {
            gameObject.layer = layer;
            foreach(var line in lines) {
                line.gameObject.layer = layer;
            }
        }
    }
}

