/* Copyright © 2013, Elián Hanisch <lambdae2@gmail.com>
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
    public abstract class MassEditorMarker : EditorMarker_CoM
    {
        MassEditorMarker instance;
        protected Vector3 vectorSum;
        protected float totalMass;

        static HashSet<int> nonPhysicsModules = new HashSet<int> {
            "ModuleLandingGear".GetHashCode(),
            "LaunchClamp".GetHashCode(), /* has mass at launch, but accounting it is worthless */
        };

        static HashSet<int> nonPhysicsParts = new HashSet<int> {
            "ladder1".GetHashCode(),
            "telescopicLadder".GetHashCode(),
            "telescopicLadderBay".GetHashCode(),
        };

        public float mass {
            get { return instance.totalMass; }
        }

        public MassEditorMarker ()
        {
            instance = this;
        }

        protected override Vector3 UpdatePosition ()
        {
            vectorSum = Vector3.zero;
            totalMass = 0f;

            if (EditorLogic.startPod == null) {
                return Vector3.zero;
            }

            recursePart (EditorLogic.startPod);
            if (EditorLogic.SelectedPart != null) {
                Part part = EditorLogic.SelectedPart;
                if (part.potentialParent != null) {
                    recursePart (part);

                    List<Part>.Enumerator enm = part.symmetryCounterparts.GetEnumerator();
                    while (enm.MoveNext()) {
                        recursePart (enm.Current);
                    }
                }
            }

            return vectorSum / totalMass;
        }

        void recursePart (Part part)
        {
            if (physicalSignificance(part)){
                calculateCoM(part);
            }
           
            List<Part>.Enumerator enm = part.children.GetEnumerator();
            while (enm.MoveNext()) {
                recursePart (enm.Current);
            }
        }

        bool physicalSignificance (Part part)
        {
            if (part.physicalSignificance == Part.PhysicalSignificance.FULL) {
                if (nonPhysicsParts.Contains (part.partInfo.name.GetHashCode())) {
                    return false;
                }
                IEnumerator<PartModule> enm = (IEnumerator<PartModule>)part.Modules.GetEnumerator ();
                while (enm.MoveNext()) {
                    PartModule mod = enm.Current;
                    if (nonPhysicsModules.Contains (mod.ClassID)) {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        protected abstract void calculateCoM (Part part);
    }

    public class CoM_Marker : MassEditorMarker
    {
        static CoM_Marker instance;

        public static float Mass {
            get { return instance.totalMass; }
        }

        public CoM_Marker ()
        {
            instance = this;
        }

        protected override void calculateCoM (Part part)
        {
            float mass = part.mass + part.GetResourceMass ();

            vectorSum += (part.transform.position 
                + part.transform.rotation * part.CoMOffset)
                * mass;
            totalMass += mass;
        }
    }

    public class DCoM_Marker : MassEditorMarker
    {
        static DCoM_Marker instance;
        static Dictionary<string, float> resourceMass = new Dictionary<string, float> ();

        public static Dictionary<string, bool> resourceCfg = new Dictionary<string, bool> ();

        public static Dictionary<string, float> Resource {
            get { return resourceMass; }
        }

        public static float Mass {
            get { return instance.totalMass; }
        }

        public DCoM_Marker ()
        {
            instance = this;
            Load ();
        }

        void Load ()
        {
            /* for these resources, default to false */
            string[] L = new string[] { "LiquidFuel", "Oxidizer", "SolidFuel" };
            foreach (string name in L) {
                resourceCfg [name] = Settings.GetValue ("drycom_" + name, false);
            }
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Save ()
        {
            foreach (string name in resourceCfg.Keys) {
                Settings.SetValue ("drycom_" + name, resourceCfg [name]);
            }
        }

        protected override Vector3 UpdatePosition ()
        {
            resourceMass.Clear ();
            return base.UpdatePosition ();
        }

        protected override void calculateCoM (Part part)
        {
            float mass = part.mass;

            /* add resource mass */
            IEnumerator<PartResource> enm = (IEnumerator<PartResource>)part.Resources.GetEnumerator();
            while (enm.MoveNext()) {
                PartResource res = enm.Current;
                if (res.info.density == 0) {
                    continue;
                }
                float rMass = (float)res.amount * res.info.density;
                if (!resourceMass.ContainsKey(res.info.name)) {
                    resourceMass[res.info.name] = rMass;
                } else {
                    resourceMass[res.info.name] += rMass;
                }

                bool addResource;
                if (!resourceCfg.TryGetValue (res.info.name, out addResource)) {
                    string configName = "drycom_" + res.info.name;
                    /* if the resource starts empty, default to false */
                    addResource = Settings.GetValue(configName, res.amount == 0 ? false : true);
                    resourceCfg[res.info.name] = addResource;
                }
                if (addResource) {
                    mass += rMass;
                }
            }

            vectorSum += (part.transform.position 
                + part.transform.rotation * part.CoMOffset)
                * mass;
            totalMass += mass;
        }
    }
}
