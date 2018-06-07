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
        public virtual VectorGraphic[] vectors { get; private set; }

        protected Color color = Color.cyan;

        protected virtual void Init ()
        {
        }

        protected virtual void Cleanup()
        {
        }

        void Awake ()
        {
            vectors = new VectorGraphic[0];  /* just for avoid possible NRE */
            Init ();
            Events.LeavingEditor += onLeavingEditor;
            Events.PluginDisabled += onPluginDisabled;
            Events.PluginEnabled += onPluginEnabled;
            Events.PartChanged += onPartChanged;
            Events.ModeChanged += onModeChanged;
        }

        void Start ()
        {
            initVectors ();
            /* check state for activate module if needed */
            stateChanged (); 
        }

        void OnDestroy()
        {
            Events.LeavingEditor -= onLeavingEditor;
            Events.PluginDisabled -= onPluginDisabled;
            Events.PluginEnabled -= onPluginEnabled;
            Events.PartChanged -= onPartChanged;
            Events.ModeChanged -= onModeChanged;
            Cleanup ();
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

        protected VectorGraphic[] getVectors(int count)
        {
            GameObject obj;
            var v = new VectorGraphic[count];
            for (int i = 0; i < count; i++) {
                obj = new GameObject ("PartModule Vector object");
                obj.layer = gameObject.layer;
                v [i] = obj.AddComponent<VectorGraphic> ();
                configVector(v [i]);
            }
            return v;
        }

        protected virtual void initVectors()
        {
            /* thrusterTransforms aren't initialized while in Awake, call in Start */
            vectors = getVectors (thrustTransforms.Count);
        }

        protected virtual void destroyVectors ()
        {
            Debug.Assert (vectors != null, "Vectors weren't initialized");
            for (int i = 0; i < vectors.Length; i++) {
                if (vectors [i] != null) {
                    Destroy (vectors [i].gameObject);
                }
            }
            vectors = new VectorGraphic[0];
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
            Debug.Assert (vectors != null, "Vectors weren't initialized");
            /* we update forces positions in LateUpdate instead of parenting them to the part
             * for prevent CoM position to be out of sync */
            for (int i = thrustTransforms.Count - 1; i >= 0; i--) {
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

        /* test if this part is connected to vessel */
        protected abstract bool connectedToVessel { get; }
        /* test if this part should be active during this mode */
        protected abstract bool activeInMode (PluginMode mode);
        /* get the transforms of the forces to account */
        protected abstract List<Transform> thrustTransforms { get; }
    }
}
