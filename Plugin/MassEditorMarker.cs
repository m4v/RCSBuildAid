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
        public bool GeneralToggle = true;   /* for editor's CoM toggle button */
        public bool SettingsToggle = true; /* for RCSBA's visibility settings */

        void Awake ()
        {
            Events.PluginDisabled += onPluginDisable;
            Events.PluginEnabled += onPluginEnable;
        }

        void OnDestroy ()
        {
            Events.PluginDisabled -= onPluginDisable;
            Events.PluginEnabled -= onPluginEnable;
        }

        void LateUpdate ()
        {
            gameObject.GetComponent<Renderer> ().enabled = isVisible;
        }

        void onPluginDisable(bool byUser)
        {
            GeneralToggle = false;
        }

        void onPluginEnable(bool byUser)
        {
            GeneralToggle = true;
        }

        public bool isVisible {
            get { return GeneralToggle && SettingsToggle; }
        }

        public void Show ()
        {
            GeneralToggle = true; SettingsToggle = true;
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
            gameObject.AddComponent<MarkerVisibility> ().SettingsToggle = Settings.show_marker_com;
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
}
