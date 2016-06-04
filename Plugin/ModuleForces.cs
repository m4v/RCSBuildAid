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
    public abstract class ModuleForces : MonoBehaviour
    {
        public VectorGraphic[] vectors { get; private set; }

        protected Color color = Color.cyan;

        protected virtual void Init ()
        {
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
            destroyVectors ();
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

        protected virtual void Start ()
        {
            /* thrusterTransforms aren't initialized while in Awake, so in Start instead */
            createVectors (thrustTransforms.Count);
            stateChanged (); /* activate module if needed */
        }

        protected void createVectors(int count)
        {
            GameObject obj;
            vectors = new VectorGraphic[count];
            for (int i = 0; i < count; i++) {
                obj = new GameObject ("PartModule Vector object");
                obj.layer = gameObject.layer;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
                configVector(vectors [i]);
            }
        }

        protected void destroyVectors ()
        {
            if (vectors == null) {
                return;
            }
            for (int i = 0; i < vectors.Length; i++) {
                if (vectors [i] != null) {
                    Destroy (vectors [i].gameObject);
                }
            }
            vectors = null;
        }

        protected virtual void configVector (VectorGraphic vector)
        {
            vector.setColor (color);            
        }

        protected virtual void Update ()
        {
        }

        protected virtual void LateUpdate ()
        {
            if (vectors == null) {
                return;
            }
            /* we update forces positions in LateUpdate instead of parenting them to the part
             * for prevent CoM position to be out of sync */
            for (int i = 0; i < thrustTransforms.Count; i++) {
                vectors [i].transform.position = thrustTransforms [i].position;
            }
        }

        public void Enable ()
        {
            if (!enabled) {
                enabled = true;
                if (vectors == null) {
                    return;
                }
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = true;
                }
            }
        }

        public void Disable ()
        {
            if (enabled) {
                enabled = false;
                if (vectors == null) {
                    return;
                }
                for (int i = 0; i < vectors.Length; i++) {
                    vectors [i].enabled = false;
                }
            }
        }

        protected abstract bool connectedToVessel { get; }
        protected abstract bool activeInMode (PluginMode mode);
        protected abstract List<Transform> thrustTransforms { get; }
    }
}
