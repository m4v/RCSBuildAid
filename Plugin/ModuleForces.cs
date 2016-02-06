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
        public VectorGraphic[] vectors = new VectorGraphic[0]; // need a valid ref for avoid NRE

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
            RCSBuildAid.events.ModeChanged += onModeChanged;
        }

        void OnDestroy()
        {
            Events.LeavingEditor -= onLeavingEditor;
            Events.PluginDisabled -= onPluginDisabled;
            Events.PluginEnabled -= onPluginEnabled;
            Events.PartChanged -= onPartChanged;
            RCSBuildAid.events.ModeChanged -= onModeChanged;

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

        protected virtual void Start ()
        {
            /* thrusterTransforms aren't initialized while in Awake, so in Start instead */
            GameObject obj;
            int n = thrustTransforms.Count;
            vectors = new VectorGraphic[n];
            for (int i = 0; i < n; i++) {
                obj = new GameObject ("PartModule Vector object");
                obj.layer = gameObject.layer;
                vectors [i] = obj.AddComponent<VectorGraphic> ();
                vectors [i].setColor(color);
            }
            stateChanged (); /* activate module if needed */
        }

        protected virtual void Update ()
        {
        }

        protected virtual void LateUpdate ()
        {
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

        protected abstract bool connectedToVessel { get; }
        protected abstract bool activeInMode (PluginMode mode);
        protected abstract List<Transform> thrustTransforms { get; }
    }
}
