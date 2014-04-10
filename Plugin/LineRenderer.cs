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
using System.Diagnostics;
using UnityEngine;

namespace RCSBuildAid
{
    public static class VectorUtils
    {
        public static float calcDimentionExp (this GraphicBase line, float value, float y0, float y1)
        {
            /* exponential scaling makes changes near zero more noticeable */
            float T = 5 / line.upperMagnitude;
            float A = (y1 - y0) / Mathf.Exp(-line.lowerMagnitude * T);
            value = Mathf.Clamp(value, line.lowerMagnitude, line.upperMagnitude);
            return y1 - A * Mathf.Exp(-value * T);
        }

        public static float calcDimentionLinear (this GraphicBase line, float value, float y0, float y1)
        {
            float m = (y1 - y0) / (line.upperMagnitude - line.lowerMagnitude);
            float b = y1 - line.upperMagnitude * m;
            value = Mathf.Clamp(value, line.lowerMagnitude, line.upperMagnitude);
            return value * m + b;
        }
    }

    public class GraphicBase : MonoBehaviour
    {
        public float upperMagnitude = 2;
        public float lowerMagnitude = 0.01f;
    }

    [RequireComponent(typeof(LineRenderer))]
    public class VectorGraphic : GraphicBase
    {
        //string shader = "GUI/Text Shader"; /* solid and on top of everything in that layer */
        public static string shader = "Particles/Alpha Blended"; /* solid */
        //public static string shader = "Particles/Additive";

        public Vector3 value = Vector3.zero;
        public Vector3 valueTarget = Vector3.zero;
        public float offset = 0;
        public float maxLength = 1.5f;
        public float minLength = 0.1f;
        public float maxWidth = 0.05f;
        public float minWidth = 0.02f;
        public bool exponentialScale = false;

        public Vector3 startPoint { get; private set; }
        public Vector3 endPoint { get; private set; }

        Material material;

        /* Need SerializeField or clonning will fail to pick these private variables */
        [SerializeField]
        LineRenderer line;
        [SerializeField]
        LineRenderer arrow;
        [SerializeField]
        LineRenderer target;
        [SerializeField]
        GUIText debugLabel;

        public new bool enabled {
            get { return base.enabled; }
            set {
                if (base.enabled == value) {
                    return;
                }
                base.enabled = value;
                enableLines(value);
            }
        }

        void enableLines (bool value)
        {
            line.enabled = value;
            arrow.enabled = value;
            if (target != null) {
                target.enabled = value;
            }
            enableDebugLabel (value);
        }

        [Conditional("DEBUG")]
        void enableDebugLabel ( bool value) {
            if (debugLabel != null) {
                debugLabel.enabled = value;
            }
        }

        [SerializeField]
        Color _color = Color.cyan;
        public Color color {
            get { return _color; }
            set {
                _color = value;
                line.SetColors (_color, _color);
                arrow.SetColors (_color, _color);
                if (target != null) {
                    target.SetColors (_color, _color);
                }
            }
        }

        float _width = 0.03f;
        public float width {
            get { return _width; }
            set {
                _width = value;
                line.SetWidth (_width, _width);
                arrow.SetWidth (_width * 3, 0);
                if (target != null) {
                    target.SetWidth (0, width);
                }
            }
        }

        public int layer {
            get { return gameObject.layer; }
            set {
                gameObject.layer = value;
                arrow.gameObject.layer = value;
                if (target != null) {
                    target.gameObject.layer = value;
                }
            }
        }

        LineRenderer newLine ()
        {
            GameObject obj = new GameObject("LineRenderer object");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            LineRenderer line = obj.AddComponent<LineRenderer>();
            line.material = material;
            return line;
        }

        void Awake ()
        {
            material = new Material (Shader.Find (shader));
            line = GetComponent<LineRenderer> ();
            line.material = material;

            /* arrow point */
            if (arrow == null) {
                arrow = newLine ();
            }

        }

        void Start ()
        {
            line.SetVertexCount (2);
            line.SetColors(color, color);
            line.SetWidth (width, width);

            arrow.SetVertexCount(2);
            arrow.SetColors(color, color);
            arrow.SetWidth(width * 3, 0);
        }

        void LateUpdate ()
        {
            if (value.magnitude < lowerMagnitude) {
                enableLines (false);
                return;
            } else {
                enableLines (true);
            }

            /* calc dimentions */
            float lenght = 0f;
            if (!exponentialScale) {
                lenght = this.calcDimentionLinear (value.magnitude, minLength, maxLength);
                width = this.calcDimentionLinear (value.magnitude, minWidth, maxWidth);
            } else {
                lenght = this.calcDimentionExp (value.magnitude, minLength, maxLength);
                width = this.calcDimentionExp (value.magnitude, minWidth, maxWidth);
            }

            Vector3 norm = value.normalized;

            startPoint = transform.position + norm * offset;
            endPoint = startPoint + norm * lenght;
            Vector3 dir = endPoint - startPoint;

            /* calculate arrow tip lenght */
            float arrowL = Mathf.Clamp (dir.magnitude / 2f, 0f, width * 4);
            Vector3 midPoint = endPoint - dir.normalized * arrowL;

            line.SetPosition (0, startPoint);
            line.SetPosition (1, midPoint);

            arrow.SetPosition (0, midPoint);
            arrow.SetPosition (1, endPoint);

            /* target marker */
            if ((valueTarget != Vector3.zero) && enabled) {
                if (target == null) {
                    setupTargetMarker ();
                }
                Vector3 p1 = startPoint + (valueTarget.normalized * lenght);
                Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
                target.SetPosition (0, p1);
                target.SetPosition (1, p2);
                target.enabled = true;
            } else if (target != null) {
                target.enabled = false;
            }

            showDebugLabel ();
        }

        [Conditional("DEBUG")]
        void showDebugLabel ()
        {
            if (DebugSettings.labelMagnitudes) {
                if (debugLabel == null) {
                    GameObject obj = new GameObject ("VectorGraphic debug label");
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
                    debugLabel.text = "";
                }
            } else {
                if (debugLabel != null) {
                    debugLabel.enabled = false;
                }
            }
        }

        void setupTargetMarker ()
        {
            target = newLine();
            target.SetVertexCount(2);
            target.SetColors(color, color);
            target.SetWidth (0, width);
            target.enabled = false;
        }
    }

    [RequireComponent(typeof(LineRenderer))]
    public class CircularVectorGraphic : GraphicBase
    {
        public float minRadius = 0.6f;
        public float maxRadius = 3f;
        public float maxWidth = 0.16f;
        public float minWidth = 0.02f;
        public int vertexCount = 48;
        public Vector3 value = Vector3.zero;

        LineRenderer line;
        LineRenderer arrow;
        Material material = new Material(Shader.Find (VectorGraphic.shader));

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                line.enabled = value;
                arrow.enabled = value;
            }
        }

        float _width = 0.08f;
        public float width {
            get { return _width; }
            set {
                _width = value;
                line.SetWidth (_width, _width);
                arrow.SetWidth (_width * 3, 0);
            }
        }

        void Awake () {
            upperMagnitude = 1;
            lowerMagnitude = 0.01f;

            Color circleColor = Color.red;
            circleColor.a = 0.5f;
            line = GetComponent<LineRenderer>();
            line.material = material;
            line.SetVertexCount(vertexCount - 3);
            line.useWorldSpace = false;
            line.SetColors(circleColor, circleColor);

            GameObject obj = new GameObject("CircleArrow");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;
            arrow = obj.AddComponent<LineRenderer>();
            arrow.material = material;
            arrow.SetVertexCount(2);
            arrow.useWorldSpace = false;
            arrow.SetColors(circleColor, circleColor);
        }

        void LateUpdate ()
        {
            float angAcc = value.magnitude;
            if (angAcc < lowerMagnitude) {
                line.enabled = false;
                arrow.enabled = false;
            } else {
                /* calc radius */
                float radius = this.calcDimentionExp(angAcc, minRadius, maxRadius);

                /* calc width */
                width = this.calcDimentionExp(angAcc, minWidth, maxWidth);

                /* Draw our circle */
                float angle = 2 * Mathf.PI / vertexCount;
                float pha = Mathf.PI * 4f/9f; /* phase angle, so the circle starts and ends at the
                                                 translation vector */
                Func<float, float, float> calcx = (a, r) => r * Mathf.Cos( a - pha);
                Func<float, float, float> calcy = (a, r) => r * Mathf.Sin(-a + pha);
                float x = 0, y = 0, z = 0;
                Vector3 v = Vector3.zero;
                int i = 0;
                for (; i < vertexCount - 3; i++) {
                    x = calcx(angle * i, radius);
                    y = calcy(angle * i, radius);
                    v = new Vector3(x, y, z);
                    line.SetPosition(i, v);
                }

                /* Finish with arrow */
                arrow.SetPosition(0, v);
                /* do the math for get the arrow tip tangent to the circle, we do this so
                 * it doesn't look too broken */
                float radius2 = radius / Mathf.Cos(angle * 2);
                arrow.SetPosition(1, new Vector3(calcx (angle * (i + 1), radius2),
                                                 calcy (angle * (i + 1), radius2),
                                                 z));
                arrow.enabled = true;
                line.enabled = true;
            }
        }
    }
}

