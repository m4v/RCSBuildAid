using System;
using UnityEngine;

namespace RCSBuildAid
{
    public class VectorGraphic : MonoBehaviour
    {
        public Vector3 value = Vector3.zero;
        public Vector3 valueTarget = Vector3.zero;
        public float scale = 1;
        public float maxLength = 4;
        public new bool enabled = false;
        string shader = "GUI/Text Shader";
        Material material;

        Color _color = Color.cyan;
        float _width = 0.03f;

        LineRenderer line;
        LineRenderer arrow;
        LineRenderer target;

        public Color color {
            get { return _color; }
            set {
                _color = value;
                if (line == null)
                    throw new Exception ("line is null");
                if (arrow == null)
                    throw new Exception ("arrow is null");
                line.SetColors (_color, _color);
                arrow.SetColors (_color, _color);
                if (target != null) {
                    target.SetColors (_color, _color);
                }
            }
        }

        public float width {
            get { return _width; }
            set {
                _width = value;
                if (line == null)
                    throw new Exception ("line is null");
                if (arrow == null)
                    throw new Exception ("arrow is null");
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
            line.material = material;
            return line;
        }

        void Awake ()
        {
            material = new Material (Shader.Find (shader));
            
            /* try GetComponent fist, symmetry/clonning adds LineRenderer beforehand. */
            line = GetComponent<LineRenderer> ();
            if (line == null) {
                line = gameObject.AddComponent<LineRenderer> ();
            }
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
            line.enabled = false;

            arrow.SetVertexCount(2);
            arrow.SetColors(color, color);
            arrow.SetWidth(width * 3, 0);
            arrow.enabled = false;
        }

        void LateUpdate ()
        {
            Vector3 v = value;
            if (maxLength > 0 && value.magnitude > maxLength) {
                v = value * (maxLength / value.magnitude);
            }

            Vector3 pStart = transform.position;
            Vector3 pEnd = pStart + (v * scale);
            Vector3 dir = pEnd - pStart;

            /* calculate arrow tip lenght */
            float arrowL = Mathf.Clamp (dir.magnitude / 2f, 0f, width * 4);
            Vector3 pMid = pEnd - dir.normalized * arrowL;

            line.SetPosition (0, pStart);
            line.SetPosition (1, pMid);
            line.enabled = enabled;

            arrow.SetPosition (0, pMid);
            arrow.SetPosition (1, pEnd);
            arrow.enabled = enabled;

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
    }
}

