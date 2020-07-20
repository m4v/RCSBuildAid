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

using UnityEngine;
using UnityEngine.Profiling;

namespace RCSBuildAid
{
    public class MarkerScaler : MonoBehaviour
    {
        public float scale = 1f;
        const float distScale = 0.1f;

        void LateUpdate ()
        {
            Profiler.BeginSample("[RCSBA] MarkerScaler LateUpdate");
            float v = scale * Settings.marker_scale;
            if (Settings.marker_autoscale) {
                var camTransform = EditorCamera.Instance.transform;
                var plane = new Plane (camTransform.forward, camTransform.position);
                float dist = plane.GetDistanceToPoint (transform.position);
                v *= Mathf.Clamp (distScale * dist, 0f, 1f);
            }
            transform.localScale = Vector3.one * v;
            Profiler.EndSample();
        }
    }

    public class MarkerVisibility : MonoBehaviour
    {
        public bool generalToggle;  /* for editor's CoM toggle button */
        public bool settingsToggle; /* for RCSBA's visibility settings */

        Renderer renderer;

        void Awake()
        {
            generalToggle = RCSBuildAid.Enabled;
            settingsToggle = Settings.show_marker_com;
        }

        void Start()
        {
            renderer = GetComponent<Renderer>();
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
            renderer.enabled = Visible;
        }

        void onPluginDisable(bool byUser)
        {
            generalToggle = false;
        }

        void onPluginEnable(bool byUser)
        {
            generalToggle = true;
        }

        public bool Visible {
            get { return generalToggle && settingsToggle; }
        }

        public void Show ()
        {
            generalToggle = true; settingsToggle = true;
        }
    }

    public abstract class MassEditorMarker : EditorMarker_CoM
    {
        protected Vector3 vectorSum;
        protected float totalMass;

        protected MarkerScaler scaler;

        public float mass {
            get { return totalMass; }
        }

        protected virtual void Awake ()
        {
            scaler = gameObject.AddComponent<MarkerScaler> ();
            gameObject.AddComponent<MarkerVisibility> ().settingsToggle = Settings.show_marker_com;
        }

        protected override Vector3 UpdatePosition ()
        {
            vectorSum = Vector3.zero;
            totalMass = 0f;

            EditorUtils.RunOnVesselParts (calculateCoM);
            EditorUtils.RunOnSelectedParts(calculateCoM);

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (totalMass == 0) {
                return Vector3.zero;
            }

            return vectorSum / totalMass;
        }

        protected abstract void calculateCoM (Part part);
    }
}
