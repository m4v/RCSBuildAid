/* Copyright © 2013-2015, Elián Hanisch <lambdae2@gmail.com>
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
        const float dist_c = 0.1f;

        void LateUpdate ()
        {
            float v = scale * Settings.marker_scale;
            if (Settings.marker_autoscale) {
                var cam = EditorCamera.Instance;
                var plane = new Plane (cam.transform.forward, cam.transform.position);
                float dist = plane.GetDistanceToPoint (transform.position);
                v *= Mathf.Clamp (dist_c * dist, 0f, 1f);
            }
            transform.localScale = Vector3.one * v;
        }
    }

    public class MarkerVisibility : MonoBehaviour
    {
        public bool CoMToggle = true;   /* for editor's CoM toggle button */
        public bool RCSBAToggle = true; /* for RCSBA's visibility settings */

        void LateUpdate ()
        {
            if (RCSBuildAid.CheckEnabledConditions()) {
                gameObject.renderer.enabled = isVisible;
            } else {
                gameObject.renderer.enabled = false;
            }
        }

        public bool isVisible {
            get { return CoMToggle && RCSBAToggle; }
        }

        public void Show ()
        {
            CoMToggle = true; RCSBAToggle = true;
        }
    }

    public abstract class MassEditorMarker : EditorMarker_CoM
    {
        MassEditorMarker instance;
        protected Vector3 vectorSum;
        protected float totalMass;

        protected MarkerScaler scaler;

        public float mass {
            get { return instance.totalMass; }
        }

        protected MassEditorMarker ()
        {
            instance = this;
        }

        protected virtual void Awake ()
        {
            scaler = gameObject.AddComponent<MarkerScaler> ();
            gameObject.AddComponent<MarkerVisibility> ().RCSBAToggle = Settings.show_marker_com;
        }

        protected override Vector3 UpdatePosition ()
        {
            vectorSum = Vector3.zero;
            totalMass = 0f;

            EditorUtils.RunOnAllParts (calculateCoM);

            if (vectorSum.IsZero ()) {
                return vectorSum;
            }

            return vectorSum / totalMass;
        }

        protected abstract void calculateCoM (Part part);
    }

    public class CoMMarker : MassEditorMarker
    {
        static CoMMarker instance;

        public static float Mass {
            get { return instance.totalMass; }
        }

        public CoMMarker ()
        {
            instance = this;
        }

        protected override void calculateCoM (Part part)
        {
            if (part.GroundParts ()) {
                return;
            }

            Vector3 com;
            if (part.GetCoM(out com)) {
                float m = part.GetTotalMass ();
                vectorSum += com * m;
                totalMass += m;
            }
        }
    }

    public class DCoMResource
    {
        PartResourceDefinition info;
        public double amount;

        public DCoMResource (PartResource resource)
        {
            if (resource == null) {
                throw new ArgumentNullException ("resource");
            }
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
            // Analysis disable once CompareOfFloatsByEqualityOperator
            return info.density == 0;
        }
    }

    public class DCoMMarker : MassEditorMarker
    {
        static DCoMMarker instance;

        public static Dictionary<string, DCoMResource> Resource = new Dictionary<string, DCoMResource> ();

        public static float Mass {
            get { return instance.totalMass; }
        }

        public DCoMMarker ()
        {
            instance = this;
        }

        protected override void Awake ()
        {
            base.Awake();
            scaler.scale = 0.9f;
            renderer.material.color = Color.red;
            gameObject.GetComponent<MarkerVisibility> ().RCSBAToggle = Settings.show_marker_dcom;
        }

        protected override Vector3 UpdatePosition ()
        {
            Resource.Clear ();
            return base.UpdatePosition ();
        }

        protected override void calculateCoM (Part part)
        {
            if (part.GroundParts ()) {
                return;
            }

            Vector3 com;
            if (!part.GetCoM (out com)) {
                return;
            }

            float m = part.mass;

            /* add resource mass */
            for (int i = 0; i < part.Resources.Count; i++) {
                PartResource res = part.Resources [i];
                if (!Resource.ContainsKey (res.info.name)) {
                    Resource [res.info.name] = new DCoMResource (res);
                }
                else {
                    Resource [res.info.name].amount += res.amount;
                }
                // Analysis disable once CompareOfFloatsByEqualityOperator
                if (res.info.density == 0) {
                    /* no point in toggling it off/on from the DCoM marker */
                    continue;
                }
                if (Settings.GetResourceCfg (res.info.name, false) || !res.flowState) {
                    /* if resource isn't in the cfg, is a likely a resource added by a mod
                     * so default to false */
                    m += (float)(res.amount * res.info.density);
                }
            }

            vectorSum += com * m;
            totalMass += m;
        }
    }

    public class AverageMarker : MassEditorMarker
    {
        public MassEditorMarker CoM1;
        public MassEditorMarker CoM2;

        protected override void Awake ()
        {
            base.Awake();
            scaler.scale = 0.6f;
            renderer.material.color = XKCDColors.Orange;
            gameObject.GetComponent<MarkerVisibility> ().RCSBAToggle = Settings.show_marker_acom;
        }

        protected override Vector3 UpdatePosition ()
        {
            Vector3 position = (CoM1.transform.position + CoM2.transform.position) / 2;
            totalMass = (CoM1.mass + CoM2.mass) / 2;
            return position;
        }

        protected override void calculateCoM (Part part)
        {
        }
    }
}