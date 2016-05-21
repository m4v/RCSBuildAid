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
            var color = Color.red;
            color.a = 0.5f;
            gameObject.GetComponent<Renderer> ().material.color = color;
            gameObject.GetComponent<MarkerVisibility> ().SettingsToggle = Settings.show_marker_dcom;
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

            /* add resource mass */
            for (int i = 0; i < part.Resources.Count; i++) {
                PartResource res = part.Resources [i];
                if (!Resource.ContainsKey (res.info.name)) {
                    Resource [res.info.name] = new DCoMResource (res);
                } else {
                    Resource [res.info.name].amount += res.amount;
                }
            }

            /* calculate DCoM */
            float m = part.GetSelectedMass();

            vectorSum += com * m;
            totalMass += m;
        }
    }
}
