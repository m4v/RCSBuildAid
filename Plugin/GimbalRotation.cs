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
using UnityEngine;

namespace RCSBuildAid
{
    public class GimbalRotation : MonoBehaviour
    {
        [SerializeField]
        ModuleGimbal gimbal;
        [SerializeField]
        Quaternion[] initRots;
        [SerializeField]
        float startTime;

        const float speed = 2f;

        void Awake ()
        {
            RCSBuildAid.events.DirectionChanged += switchDirection;
        }

        void OnDestroy ()
        {
            RCSBuildAid.events.DirectionChanged -= switchDirection;
        }

        public static void addTo(GameObject obj)
        {
            if (obj.GetComponent<GimbalRotation> () != null) {
                /* already added */
                return;
            }
            var gimbals = obj.GetComponents<ModuleGimbal> ();
            for (int i = 0; i < gimbals.Length; i++) {
                var g = obj.AddComponent<GimbalRotation> ();
                g.gimbal = gimbals [i];
            }
        }

        void Start ()
        {
            if (gimbal != null && initRots == null) {
                initRots = new Quaternion[gimbal.gimbalTransforms.Count];
                for (int i = 0; i < gimbal.gimbalTransforms.Count; i++) {
                    initRots [i] = gimbal.gimbalTransforms [i].localRotation;
                }
            }
        }

        void switchDirection (Direction direction)
        {
            /* for the animation */
            startTime = Time.time;
        }

        float getGimbalRange ()
        {
            return gimbal.gimbalRange * gimbal.gimbalLimiter / 100f;
        }

        void Update ()
        {
            if (gimbal == null) {
                return;
            }
            for (int i = 0; i < gimbal.gimbalTransforms.Count; i++) {
                Transform t = gimbal.gimbalTransforms [i];
                Quaternion finalRotation;
                if (gimbal.gimbalLock || (gimbal.part.inverseStage != RCSBuildAid.LastStage) 
                    || (RCSBuildAid.Mode != PluginMode.Engine)) {
                    finalRotation = initRots [i];
                } else {
                    float angle = getGimbalRange ();
                    Vector3 pivot;
                    switch (RCSBuildAid.Direction) {
                    /* forward and back are the directions for roll when in attitude modes */
                    case Direction.forward:
                        angle *= -1; /* roll left */
                        goto roll_calc;
                    case Direction.back:
                        roll_calc:
                        Vector3 vessel_up = RCSBuildAid.RotationVector;
                        Vector3 dist = t.position - RCSBuildAid.ReferenceMarker.transform.position;
                        pivot = dist - Vector3.Dot (dist, vessel_up) * vessel_up;
                        if (pivot.sqrMagnitude > 0.01) {
                            pivot = t.InverseTransformDirection (pivot);
                            finalRotation = initRots [i] * Quaternion.AngleAxis (angle, pivot);
                        } else {
                            finalRotation = initRots [i];
                        }
                        break;
                    default:
                        pivot = t.InverseTransformDirection (RCSBuildAid.RotationVector);
                        finalRotation = initRots [i] * Quaternion.AngleAxis (angle, pivot);
                        break;
                    }
                }
                t.localRotation = Quaternion.Lerp (t.localRotation, finalRotation, (Time.time - startTime) * speed);
            }
        }
    }
}

