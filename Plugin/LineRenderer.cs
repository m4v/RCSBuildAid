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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public class LineBase : MonoBehaviour
    {
        //string shader = "GUI/Text Shader"; /* solid and on top of everything in that layer */
        public const string shader = "Particles/Alpha Blended"; /* solid */
        //public static string shader = "Particles/Additive";

        /* Need SerializeField or clonning will fail to pick these private variables */
        [SerializeField]
        protected Color color = Color.cyan;
        [SerializeField]
        protected List<LineRenderer> lines = new List<LineRenderer> ();
        protected Material material;

        const int layer = 1; /* overlay layer */

        public virtual void setColor (Color value) {
            color = value;
            foreach(var line in lines) {
                line.SetColors (value, value);
            }
        }

        public virtual void setWidth(float v1, float v2) {
            foreach(var line in lines) {
                line.SetWidth (v1, v2);
            }
        }

        public virtual void setWidth (float v2)
        {
            setWidth (v2, v2);
        }

        public new virtual bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                enableLines (value);
            }
        }

        protected virtual void enableLines (bool value)
        {
            foreach(var line in lines) {
                line.enabled = value;
            }
        }

        protected LineRenderer newLine ()
        {
            var obj = new GameObject("RCSBuildAid LineRenderer object");
            var lr = obj.AddComponent<LineRenderer>();
            obj.transform.parent = gameObject.transform;
            obj.transform.localPosition = Vector3.zero;
            lr.material = material;
            return lr;
        }

        protected virtual void Awake ()
        {
            material = new Material (Shader.Find (shader));
        }

        protected virtual void Start ()
        {
            setColor (color);
            setLayer ();
        }

        protected virtual void LateUpdate ()
        {
            checkLayer ();
        }

        void checkLayer ()
        {
            /* the Editor clobbers the layer's value whenever you pick the part */
            if (gameObject.layer != layer) {
                setLayer ();
            }
        }

        public virtual void setLayer ()
        {
            gameObject.layer = layer;
            foreach(var line in lines) {
                line.gameObject.layer = layer;
            }
        }
    }

    [RequireComponent(typeof(LineRenderer))]
    public class ArrowBase : LineBase
    {
        /* magnitude limits for the graphical representation */
        public float upperMagnitude = 2;
        public float lowerMagnitude = 0.01f;

        public Vector3 value = Vector3.zero;

        protected LineRenderer line;
        protected LineRenderer lineEnd;

        bool holdUpdate = true;

        public override bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                if (!holdUpdate || !value) {
                    enableLines (value);
                }
            }
        }

        public override void setWidth (float v2)
        {
            line.SetWidth (v2, v2);
            lineEnd.SetWidth (v2 * 3, 0);
        }

        protected override void Awake ()
        {
            base.Awake ();
            if (lines.Count == 0) {
                line = GetComponent<LineRenderer> ();
                line.material = material;
                lines.Add (line);

                /* arrow point */
                lineEnd = newLine ();
                lines.Add (lineEnd);
            } else {
                line = lines [0];
                lineEnd = lines [1];
            }

            RCSBuildAid.events.ModeChanged += onModeChange;
        }

        void OnDestroy ()
        {
            RCSBuildAid.events.ModeChanged -= onModeChange;
        }

        protected float calcDimentionExp (float miny, float maxy)
        {
            /* exponential scaling makes changes near zero more noticeable */
            float T = 5 / upperMagnitude;
            float A = (maxy - miny) / Mathf.Exp(-lowerMagnitude * T);
            float v = Mathf.Clamp(value.magnitude, lowerMagnitude, upperMagnitude);
            return maxy - A * Mathf.Exp(-v * T);
        }

        protected float calcDimentionLinear (float miny, float maxy)
        {
            float m = (maxy - miny) / (upperMagnitude - lowerMagnitude);
            float b = maxy - upperMagnitude * m;
            float v = Mathf.Clamp(value.magnitude, lowerMagnitude, upperMagnitude);
            return v * m + b;
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();
            enableLines (!holdUpdate && (value.magnitude >= lowerMagnitude));
            holdUpdate = false;
        }

        void onModeChange (PluginMode mode)
        {
            holdUpdate = true;
        }
    }

    public class VectorGraphic : ArrowBase
    {
        public float offset;
        public float maxLength = 1.5f;
        public float minLength = 0.1f;
        public float maxWidth = 0.05f;
        public float minWidth = 0.02f;

        public Vector3 startPoint { get; private set; }
        public Vector3 endPoint { get; private set; }

        protected float lenght;
        protected float width;

        [SerializeField]
        GUIText debugLabel;

        protected override void enableLines (bool value)
        {
            base.enableLines (value);
            enableDebugLabel (value);
        }

        [Conditional("DEBUG")]
        void enableDebugLabel (bool v) {
            if (debugLabel != null) {
                debugLabel.enabled = v;
            }
        }

        protected virtual void calcDimentions (out float lenght, out float width) {
            lenght = calcDimentionLinear (minLength, maxLength);
            width = calcDimentionLinear (minWidth, maxWidth);
        }

        protected override void Awake ()
        {
            base.Awake ();
            line.SetVertexCount (2);
            lineEnd.SetVertexCount(2);
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();

            if (line.enabled) {
                /* calc dimentions */
                calcDimentions (out lenght, out width);

                setWidth (width);

                Vector3 norm = value.normalized;

                startPoint = transform.position + norm * offset;
                endPoint = startPoint + norm * lenght;
                Vector3 dir = endPoint - startPoint;

                /* calculate arrow tip lenght */
                float arrowL = Mathf.Clamp (dir.magnitude / 2f, 0f, width * 4);
                Vector3 midPoint = endPoint - dir.normalized * arrowL;

                line.SetPosition (0, startPoint);
                line.SetPosition (1, midPoint);

                lineEnd.SetPosition (0, midPoint);
                lineEnd.SetPosition (1, endPoint);

                showDebugLabel ();
            }
        }

        [Conditional("DEBUG")]
        void showDebugLabel ()
        {
            if (DebugSettings.labelMagnitudes) {
                if (debugLabel == null) {
                    var obj = new GameObject ("VectorGraphic debug label");
                    obj.transform.parent = transform;
                    debugLabel = obj.AddComponent<GUIText> ();
                }
                debugLabel.enabled = true;
                debugLabel.transform.position = 
                    EditorLogic.fetch.editorCamera.WorldToViewportPoint (endPoint);
                if (value.magnitude > 0f) {
                    Vector3 lever = RCSBuildAid.ReferenceMarker.transform.position - transform.position;
                    float angle = Vector3.Angle(lever, value) * Mathf.Deg2Rad;
                    debugLabel.text = String.Format ("force: {0:0.##}\nlever: {1:0.##}\nsin: {2:0.##}", 
                                                     value.magnitude, lever.magnitude, Mathf.Sin (angle));
                } else {
                    debugLabel.text = String.Empty;
                }
            } else {
                if (debugLabel != null) {
                    debugLabel.enabled = false;
                }
            }
        }
    }

    public class MarkerVectorGraphic : VectorGraphic
    {
        public Vector3 valueTarget = Vector3.zero;

        LineRenderer target;

        protected override void Awake ()
        {
            base.Awake ();
            if (lines.Count == 2) {
                target = newLine ();
                lines.Add (target);
            } else {
                target = lines [2];
            }
        }

        protected override void Start ()
        {
            base.Start ();

            target.gameObject.layer = gameObject.layer;
            target.SetVertexCount (2);
            target.SetColors (color, color);

            offset = 0.6f;
            maxLength = 3f;
            minLength = 0.25f;
            maxWidth = 0.16f;
            minWidth = 0.05f;
            upperMagnitude = 5;
            lowerMagnitude = 0.05f;
        }

        protected override void calcDimentions (out float lenght, out float width)
        {
            lenght = calcDimentionLinear (minLength, maxLength);
            width = calcDimentionLinear (minWidth, maxWidth);
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();

            if (line.enabled) {
                /* target marker */
                if (valueTarget != Vector3.zero) {
                    target.SetWidth (0, width);
                    Vector3 p1 = startPoint + (valueTarget.normalized * lenght);
                    Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
                    target.SetPosition (0, p1);
                    target.SetPosition (1, p2);
                    target.enabled = true;
                } else {
                    target.enabled = false;
                }
            }
        }
    }

    public class CircularVectorGraphic : ArrowBase
    {
        public float minRadius = 0.6f;
        public float maxRadius = 3f;
        public float maxWidth = 0.16f;
        public float minWidth = 0.02f;
        public int vertexCount = 48;

        protected override void Start ()
        {
            upperMagnitude = 1;
            lowerMagnitude = 0.01f;

            Color circleColor = Color.red;
            circleColor.a = 0.5f;
            line.SetVertexCount(vertexCount - 3);
            line.useWorldSpace = false;
            line.SetColors(circleColor, circleColor);

            lineEnd.SetVertexCount(2);
            lineEnd.useWorldSpace = false;
            lineEnd.SetColors(circleColor, circleColor);

            lineEnd.gameObject.layer = gameObject.layer;
        }

        protected override void LateUpdate ()
        {
            base.LateUpdate ();

            if (line.enabled) {
                /* calc width */
                float width = calcDimentionExp(minWidth, maxWidth);
                setWidth (width);

                /* calc radius */
                float radius = calcDimentionExp(minRadius, maxRadius);

                /* Draw our circle */
                float angle = 2 * Mathf.PI / vertexCount;
                const float pha = Mathf.PI * 4f / 9f; /* phase angle, so the circle starts and ends at the
                                                 translation vector */
                Func<float, float, float> calcx = (a, r) => r * Mathf.Cos( a - pha);
                Func<float, float, float> calcy = (a, r) => r * Mathf.Sin(-a + pha);
                float x, y, z = 0;
                Vector3 v = Vector3.zero;
                int i = 0;
                for (; i < vertexCount - 3; i++) {
                    x = calcx(angle * i, radius);
                    y = calcy(angle * i, radius);
                    v = new Vector3(x, y, z);
                    line.SetPosition(i, v);
                }

                /* Finish with arrow */
                lineEnd.SetPosition(0, v);
                /* do the math for get the arrow tip tangent to the circle, we do this so
                 * it doesn't look too broken */
                float radius2 = radius / Mathf.Cos(angle * 2);
                lineEnd.SetPosition(1, new Vector3(calcx (angle * (i + 1), radius2),
                                                 calcy (angle * (i + 1), radius2),
                                                 z));
            }
        }
    }

    public class MarkGraphic : LineBase
    {
        const float scale = 1.2f;

        protected override void Awake ()
        {
            base.Awake ();
            for (int i = 0; i < 6; i++) {
                lines.Add (newLine ());
            }
            setWidth (0, 0.05f);
            foreach (var lineRenderer in lines) {
                lineRenderer.useWorldSpace = false;
                lineRenderer.SetVertexCount (2);
                lineRenderer.SetPosition (0, Vector3.zero);
            }

            lines[0].SetPosition (1, Vector3.forward * scale);
            lines[1].SetPosition (1, Vector3.forward * -scale);
            lines[2].SetPosition (1, Vector3.right * scale);
            lines[3].SetPosition (1, Vector3.right * -scale);
            lines[4].SetPosition (1, Vector3.up * scale);
            lines[5].SetPosition (1, Vector3.up * -scale);
        }
    }
}

