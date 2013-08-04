using System;
using UnityEngine;

namespace RCSBuildAid
{
    [RequireComponent(typeof(LineRenderer))]
    public class VectorGraphic : MonoBehaviour
    {
        public Vector3 value = Vector3.zero;
        public Vector3 valueTarget = Vector3.zero;
        public float offset = 0;
        public float maxLength = 3;
        //string shader = "GUI/Text Shader"; /* solid and on top of everything in that layer */
        string shader = "Particles/Alpha Blended"; /* solid */
        //string shader = "Particles/Additive";
        Material material;

        LineRenderer line;
        LineRenderer arrow;
        LineRenderer target;

        public new bool enabled {
            get { return base.enabled; }
            set {
                base.enabled = value;
                line.enabled = value;
                arrow.enabled = value;
            }
        }

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

        LineRenderer newLine ()
        {
            GameObject obj = new GameObject("LineRenderer object");
            LineRenderer line = obj.AddComponent<LineRenderer>();
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            line.material = material;
            return line;
        }

        void Awake ()
        {
            material = new Material (Shader.Find (shader));
            line = GetComponent<LineRenderer> ();
            line.material = material;

            /* arrow point */
            /* NOTE: when clonning the arrow is copied too and the
             * following causes to get a floating arrow around.
             * This doesn't happen now because VectorGraphics are
             * destroyed in RCSForce during clonning/symmetry. */
            arrow = newLine();
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
            Vector3 v = value;
            if (maxLength > 0 && value.magnitude > maxLength) {
                v = value * (maxLength / value.magnitude);
            }

            Vector3 pStart = transform.position + v.normalized * offset;
            Vector3 pEnd = pStart + v;
            Vector3 dir = pEnd - pStart;

            /* calculate arrow tip lenght */
            float arrowL = Mathf.Clamp (dir.magnitude / 2f, 0f, width * 4);
            Vector3 pMid = pEnd - dir.normalized * arrowL;

            line.SetPosition (0, pStart);
            line.SetPosition (1, pMid);

            arrow.SetPosition (0, pMid);
            arrow.SetPosition (1, pEnd);

            /* target marker */
            if ((valueTarget != Vector3.zero) && enabled) {
                if (target == null) {
                    setupTargetMarker();
                }
                Vector3 p1 = pStart + (valueTarget.normalized * (float)v.magnitude);
                Vector3 p2 = p1 + (valueTarget.normalized * 0.3f);
                target.SetPosition (0, p1);
                target.SetPosition (1, p2);
                target.enabled = true;
            } else if (target != null) {
                target.enabled = false;
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

        void OnDestroy ()
        {
            Destroy (line);
            Destroy (arrow.gameObject);
            if (target != null) {
                Destroy (target.gameObject);
            }
        }
    }

    [RequireComponent(typeof(LineRenderer))]
    public class TorqueGraphic : MonoBehaviour
    {
        public float minRadius = 0.6f;
        public float maxRadius = 3f;
        public float maxWidth = 0.4f;
        public int vertexCount = 36;
        public Vector3 value = Vector3.zero;
        public Vector3 valueTarget = Vector3.zero;

        LineRenderer line;
        LineRenderer arrow;
        Material material = new Material(Shader.Find("Particles/Alpha Blended"));
        VectorGraphic vector;

        public new bool enabled {
            get { return base.enabled; }
            set { 
                base.enabled = value;
                line.enabled = value;
                arrow.enabled = value;
                vector.enabled = value;
            }
        }

        float _width = 0.2f;
        public float width {
            get { return _width; }
            set {
                _width = value;
                line.SetWidth (_width * 0.4f, _width * 0.4f);
                arrow.SetWidth (_width, 0);
                vector.width = _width * 0.4f;
            }
        }

        void Awake () {
            Color circleColor = Color.red;
            line = GetComponent<LineRenderer>();
            line.material = material;
            line.SetVertexCount(vertexCount - 2);
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

            obj = new GameObject("TorqueVector");
            obj.layer = gameObject.layer;
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;
            vector = obj.AddComponent<VectorGraphic>();
            vector.value = value;
            vector.offset = 0.6f;
            vector.maxLength = 3f;
            vector.color = XKCDColors.RustRed;

            width = _width;
        }

        void LateUpdate ()
        {
            float norm = value.magnitude;
            float radius = Mathf.Clamp (norm, minRadius, maxRadius);
            if (norm < minRadius) {
                width = norm * (maxWidth / minRadius);
            } else if (width != maxWidth) {
                width = maxWidth;
            }

            vector.value = value;
            vector.valueTarget = valueTarget;

            /* Draw our circle */
            float angle = 2 * Mathf.PI / vertexCount;
            float pha = Mathf.PI * 4/9; /* phase angle, for start right at the translation vector */
            Func<float, float, float> calcx = (a, r) => r * Mathf.Cos( a - pha);
            Func<float, float, float> calcy = (a, r) => r * Mathf.Sin(-a + pha);
            float x = 0, y = 0, z = 0;
            Vector3 v = Vector3.zero;
            int i = 0;
            for (; i < vertexCount - 2; i++) {
                x = calcx(angle * i, radius);
                y = calcy(angle * i, radius);
                v = new Vector3(x, y, z);
                line.SetPosition(i, v);
            }

            /* Finish with arrow */
            arrow.SetPosition(0, v);
            /* do the math for get the arrow tip tangent to the circle, we do this so
             * it doesn't look too broken */
            float radius2 = radius / Mathf.Cos(angle);
            arrow.SetPosition(1, new Vector3(calcx (angle * i, radius2),
                                             calcy (angle * i, radius2),
                                             z));
        }

        void OnDestroy() {
            /* why do I have to do this? */
            Destroy (line);
            Destroy (arrow.gameObject);
            Destroy (vector.gameObject);
        }
    }
}

