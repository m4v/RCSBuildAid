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
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class MarkerScaler : MonoBehaviour
    {
        public float scale = 1f;

        void Update ()
        {
            transform.localScale = Vector3.one * scale * Settings.marker_scale;
        }
    }

    public abstract class MassEditorMarker : EditorMarker_CoM
    {
        MassEditorMarker instance;
        protected Vector3 vectorSum;
        protected float totalMass;

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
            if (part.physicalSignificance()){
                calculateCoM(part);
            }
           
            List<Part>.Enumerator enm = part.children.GetEnumerator();
            while (enm.MoveNext()) {
                recursePart (enm.Current);
            }
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

        void Awake ()
        {
            gameObject.AddComponent<MarkerScaler> ();
        }

        protected override void calculateCoM (Part part)
        {
            float mass = part.GetTotalMass();

            vectorSum += (part.transform.position 
                + part.transform.rotation * part.CoMOffset)
                * mass;
            totalMass += mass;
        }
    }

    public class DCoMResource
    {
        PartResourceDefinition info;
        public double amount = 0f;

        public DCoMResource (PartResource resource)
        {
            info = resource.info;
            amount = resource.amount;
        }

        public double mass {
            get { return amount * info.density; }
        }

        public string name {
            get { return info.name; }
        }

        public bool isMassless () {
            return info.density == 0;
        }
    }

    public class DCoM_Marker : MassEditorMarker
    {
        static DCoM_Marker instance;

        public static Dictionary<string, DCoMResource> Resource = new Dictionary<string, DCoMResource> ();

        public static float Mass {
            get { return instance.totalMass; }
        }

        public DCoM_Marker ()
        {
            instance = this;
        }

        void Awake ()
        {
            MarkerScaler scaler = gameObject.AddComponent<MarkerScaler> ();
            scaler.scale = 0.9f;
            renderer.material.color = Color.red;
        }

        protected override Vector3 UpdatePosition ()
        {
            Resource.Clear ();
            return base.UpdatePosition ();
        }

        protected override void calculateCoM (Part part)
        {
            float mass = part.mass;

            /* add resource mass */
            IEnumerator<PartResource> enm = (IEnumerator<PartResource>)part.Resources.GetEnumerator();
            while (enm.MoveNext()) {
                PartResource res = enm.Current;
                if (!Resource.ContainsKey(res.info.name)) {
                    Resource[res.info.name] = new DCoMResource(res);
                } else {
                    Resource[res.info.name].amount += res.amount;
                }

                if (res.info.density == 0) {
                    /* no point in toggling it off/on from the DCoM marker */
                    continue;
                }

                /* if the resource starts empty, default to false */
                bool defaultValue = res.amount == 0 ? false : true;
                if(Settings.GetResourceCfg(res.info.name, defaultValue)) {
                    mass += (float)(res.amount * res.info.density);
                }
            }

            vectorSum += (part.transform.position 
                + part.transform.rotation * part.CoMOffset)
                * mass;
            totalMass += mass;
        }
    }

    public class Average_Marker : MassEditorMarker
    {
        public MassEditorMarker CoM1;
        public MassEditorMarker CoM2;

        void Awake ()
        {
            MarkerScaler scaler = gameObject.AddComponent<MarkerScaler> ();
            scaler.scale = 0.6f;
            renderer.material.color = XKCDColors.Orange;
        }

        protected override Vector3 UpdatePosition ()
        {
            Vector3 position = (CoM1.transform.position + CoM2.transform.position) / 2;
            totalMass = (CoM1.mass + CoM2.mass) / 2;
            return position;
        }

        protected override void calculateCoM (Part part)
        {
            throw new System.NotImplementedException ();
        }
    }

    public class CoL_Marker: EditorMarker_CoL
    {
        void Awake()
        {
            gameObject.AddComponent<MarkerScaler> ();
        }
    }
};
