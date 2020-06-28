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

        public static DictionaryValueList<PartModule, ModuleForces> ModuleDict = new DictionaryValueList<PartModule, ModuleForces> ();
        public static List<ModuleForces> List = new List<ModuleForces>();

        protected Color color = Color.cyan;
        [SerializeField]
        protected PartModule module;

        public static void Add<T>(PartModule mod) where T: ModuleForces
        {
            if (ModuleDict.Contains(mod)) {
                return;
            }
            /* clonning might have created ModuleForces already, but is missing in the dict */
            T mf;
            T[] mfList = mod.gameObject.GetComponents<T> ();
            for (int i = mfList.Length - 1; i >= 0; i--) {
                mf = mfList [i];
                if (mf.module == mod) {
                    mf.enabled = true;
                    ModuleDict [mod] = mf;
                    List.Add (mf);
                    return;
                }
            }
            /* add a new ModuleForces */
            mf = mod.gameObject.AddComponent<T> ();
            mf.module = mod;
            mf.enabled = true; /* force Start() call */
            ModuleDict [mod] = mf;
            List.Add (mf);

            #if DEBUG
            Debug.Log (String.Format ("[RCSBA]: Adding {2} to {0}. Total ModuleForces count {1}.",
                mod.part.partInfo.name, List.Count, typeof(T).Name));
            #endif
        }

        protected virtual void Init ()
        {
        }

        protected virtual void Cleanup()
        {
        }

        void Awake ()
        {
            vectors = new VectorGraphic[0];  /* for avoid NRE */
        }

        void Start ()
        {
            Init ();
            initVectors ();
            /* check the state for deactivate module if needed */
            stateChanged (); 

            Events.LeavingEditor += onLeavingEditor;
            Events.PluginDisabled += onPluginDisabled;
            Events.PluginEnabled += onPluginEnabled;
            Events.PartChanged += onPartChanged;
            Events.ModeChanged += onModeChanged;
        }

        void OnDestroy()
        {
            #if DEBUG
            Debug.Log ("[RCSBA]: ModuleForces OnDestroy.");
            #endif

            Events.LeavingEditor -= onLeavingEditor;
            Events.PluginDisabled -= onPluginDisabled;
            Events.PluginEnabled -= onPluginEnabled;
            Events.PartChanged -= onPartChanged;
            Events.ModeChanged -= onModeChanged;

            List.Remove (this);
            if (module != null) {
                ModuleDict.Remove (module);
            }
            Cleanup ();
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
            Debug.Assert (module != null, "[RCSBA, ModuleForces]: module is null");

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
        }

        protected virtual void rebuildVectors()
        {
            destroyVectors();
            initVectors();
        }

        protected virtual void configVector (VectorGraphic vector)
        {
            vector.setColor (color);            
        }

        protected virtual void Update ()
        {
            Debug.Assert(thrustTransforms != null, "[RCSBA]: thrustTransforms is null");
            Debug.Assert (vectors != null, "[RCSBA]: Vectors weren't initialized");
            
            /* needed for mods like SSTU that swap models and change the number of thrustTransforms */
            if (thrustTransforms.Count != vectors.Length) {
                rebuildVectors();
            }
        }

        void LateUpdate ()
        {
            Debug.Assert(thrustTransforms != null, "[RCSBA]: thrustTransforms is null");
            Debug.Assert (vectors != null, "[RCSBA]: Vectors weren't initialized");
            Debug.Assert (vectors.Length == thrustTransforms.Count, 
                "[RCSBA]: Number of vectors doesn't match the number of transforms");

            /* we update forces positions in LateUpdate instead of parenting them to the part
             * for prevent CoM position to be out of sync */
            for (int i = thrustTransforms.Count - 1; i >= 0; i--) {
                vectors [i].transform.position = thrustTransforms [i].position;
            }
        }

        public void Enable ()
        {
            Debug.Assert (vectors != null, "[RCSBA]: Vectors weren't initialized");

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
            Debug.Assert (vectors != null, "[RCSBA]: Vectors weren't initialized");

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
