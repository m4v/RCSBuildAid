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
using UnityEngine;

namespace RCSBuildAid
{
    public abstract class PartVectors : MonoBehaviour
    {
        public VectorGraphic[] vectors = new VectorGraphic[0]; // need a valid ref for avoid NRE
        public Part part;

        protected Color color = Color.cyan;

        protected virtual void Init ()
        {
            part = GetComponent<Part> ();
            if (part == null) {
                throw new Exception ("Missing Part component");
            }
        }

        protected void Awake ()
        {
            Init ();
            Events.LeavingEditor += onLeavingEditor;
            Events.PluginDisabled += onPluginDisabled;
            Events.PluginEnabled += onPluginEnabled;
            Events.PartChanged += onPartChanged;
            Events.ModeChanged += onModeChanged;
        }

        void OnDestroy()
        {
            Events.LeavingEditor -= onLeavingEditor;
            Events.PluginDisabled -= onPluginDisabled;
            Events.PluginEnabled -= onPluginEnabled;
            Events.PartChanged -= onPartChanged;
            Events.ModeChanged -= onModeChanged;

            /* remove vectors */
            for (int i = 0; i < vectors.Length; i++) {
                if (vectors [i] != null) {
                    Destroy (vectors [i].gameObject);
                }
            }
            vectors = new VectorGraphic[0];
        }

        void onLeavingEditor ()
        {
            Disable ();
        }

        void onPluginDisabled(bool byUser)
        {
            Disable ();
        }

        void onPluginEnabled(bool byUser)
        {
            stateChanged ();
        }

        void onModeChanged (PluginMode mode)
        {
            stateChanged ();
        }

        void onPartChanged ()
        {
            stateChanged ();
        }

        void stateChanged ()
        {
            if (RCSBuildAid.Enabled && activeInMode (RCSBuildAid.Mode) && connectedToVessel) {
                Enable ();
            } else {
                Disable ();
            }
        }

        protected virtual void setupVectors (int count)
        {
            GameObject obj;
            vectors = new VectorGraphic[count];
            for (int i = 0; i < count; i++) {
                obj = new GameObject ("Vector object");
                obj.layer = gameObject.layer;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
                vectors [i].setColor(color);
            }
            stateChanged (); /* activate module if needed */
        }

        public void Enable ()
        {
            if (!enabled) {
                enabled = true;
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = true;
                }
            }
        }

        public void Disable ()
        {
            if (enabled) {
                enabled = false;
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = false;
                }
            }
        }

        protected virtual bool connectedToVessel {
            get {
                if (EditorLogic.fetch.ship.Contains (part)) {
                    return true;
                } else {
                    if (part.potentialParent != null) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }
        }

        protected abstract bool activeInMode (PluginMode mode);
    }


    public class DragCubeVector : PartVectors
    {
        protected void Start() {
            setupVectors (1);
            var v = vectors [0];
            v.upperMagnitude = 20;
            v.lowerMagnitude = 0.2f;
            v.maxLength = 2f;
            v.minLength = 0.2f;
        }

        protected virtual void Update ()
        {
            for (int i = 0; i < vectors.Length; i++) {
                var vector = vectors [i];
                var direction = -part.partTransform.TransformDirection (part.DragCubes.DragVector);
                vector.value = direction * part.DragCubes.AreaDrag;
            }
        }

        protected virtual void LateUpdate ()
        {
            Vector3 cop;
            if (part.GetCoP (out cop)) {
                if (this.part != null) {
                    for (int i = 0; i < vectors.Length; i++) {
                        vectors [i].transform.position = cop;
                    }
                }
            }
        }

        protected override bool activeInMode (PluginMode mode)
        {
            return mode == PluginMode.Parachutes;
        }
    }
}

