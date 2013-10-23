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
        protected Vector3 vectorSum;
        protected float totalMass;

        static HashSet<int> nonPhysicsModules = new HashSet<int> {
            "ModuleLandingGear".GetHashCode(),
            "LaunchClamp".GetHashCode(),
        };

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

        void Awake ()
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
        static int fuelID = "LiquidFuel".GetHashCode ();
        static int oxiID = "Oxidizer".GetHashCode ();
        static int monoID = "MonoPropellant".GetHashCode ();
        static int solidID = "SolidFuel".GetHashCode ();
        static Dictionary<int, bool> resources = new Dictionary<int, bool> ();

        public static bool other;

        public static bool fuel {
            get { return resources [fuelID]; } 
            set { resources [fuelID] = value; }
        }

        public static bool solid {
            get { return resources [solidID]; } 
            set { resources [solidID] = value; }
        }

        public static bool oxidizer {
            get { return resources [oxiID]; }
            set { resources [oxiID] = value; }
        }

        public static bool monoprop {
            get { return resources [monoID]; }
            set { resources [monoID] = value; }
        }

        public static float Mass {
            get { return instance.totalMass; }
        }

        void Awake ()
        {
            instance = this;
            Load ();
        }

        void Load ()
        {
            DCoM_Marker.other = Settings.GetValue("drycom_other", true);
            DCoM_Marker.fuel = Settings.GetValue("drycom_fuel", false);
            DCoM_Marker.monoprop = Settings.GetValue("drycom_mono", false);
            DCoM_Marker.oxidizer = DCoM_Marker.fuel;
            DCoM_Marker.solid = Settings.GetValue("drycom_solid", false);
        }

        void OnDestroy ()
        {
            Save ();
            Settings.SaveConfig();
        }

        void Save ()
        {
            Settings.SetValue ("drycom_other", DCoM_Marker.other);
            Settings.SetValue ("drycom_fuel", DCoM_Marker.fuel);
            Settings.SetValue ("drycom_solid", DCoM_Marker.solid);
            Settings.SetValue ("drycom_mono", DCoM_Marker.monoprop);
        }

        protected override void calculateCoM (Part part)
        {
            float mass = part.mass;
            IEnumerator<PartResource> enm = (IEnumerator<PartResource>)part.Resources.GetEnumerator();
            while (enm.MoveNext()) {
                PartResource res = enm.Current;
                bool addResource;
                if (resources.TryGetValue (res.info.id, out addResource)) {
                    if (addResource) {
                        mass += (float)res.amount * res.info.density;
                    }
                } else if (other) {
                    mass += (float)res.amount * res.info.density;
                }
            }

            vectorSum += (part.transform.position 
                + part.transform.rotation * part.CoMOffset)
                * mass;
            totalMass += mass;
        }
    }
}
