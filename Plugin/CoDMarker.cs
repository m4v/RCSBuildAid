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
    public class CoDMarker : EditorMarker
    {
        public static double Cd; /* drag coefficient  */
        public static double density;
        public static float altitude;
        public static double temperature;
        public static double pressure;
        public static CelestialBody body;
        public static double speed;
        public static float gravity;
        public static double Vt; /* Terminal velocity */

        static DragForce dragForce;

        Vector3 position;
        MarkGraphic mark;
        float mach;

        public static Vector3 VelocityDirection {
            get { return Vector3.down; }
        }

        public static Vector3 Velocity {
            get { return VelocityDirection * (float)speed; }
        }

        public static Vector3 DragForce {
            get { return dragForce.Vector; }
        }

        float GetDrag (Part part) {
            return part.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier;
        }

        void Awake ()
        {
            var ms = gameObject.AddComponent<MarkerScaler> ();
            ms.scale = 0.6f;
            renderer.material.color = Color.cyan;
            mark = gameObject.AddComponent<MarkGraphic> ();
            mark.setColor (Color.cyan);
            dragForce = gameObject.AddComponent<DragForce> ();
        }

        protected override Vector3 UpdatePosition ()
        {
            mark.setWidth (0, 0.05f * Settings.marker_scale);

            if (EditorLogic.RootPart == null) {
                /* DragCubes can get NaNed without this check */
                return Vector3.zero;
            }

            body = Settings.selected_body;
            altitude = MenuParachutes.altitude;
            temperature = body.GetTemperature (altitude);
            pressure = body.GetPressure (altitude);
            density = body.GetDensity (pressure, temperature);
            mach = (float)(speed / body.GetSpeedOfSound(pressure, density));
            gravity = body.gravity(altitude);

            findCenterOfDrag();
            speed = Vt = calculateTerminalVelocity ();
            dragForce.Vector = calculateDragForce ();

            return position;
        } 

        Vector3 findCenterOfDrag ()
        {
            Cd = 0f;
            position = Vector3.zero;

            /* setup parachutes */
            switch (RCSBuildAid.Mode) {
            case PluginMode.Parachutes:
                for (int i = 0; i < RCSBuildAid.Parachutes.Count; i++) {
                    var parachute = (ModuleParachute)RCSBuildAid.Parachutes [i];
                    var dc = parachute.part.DragCubes;
                    dc.SetCubeWeight ("DEPLOYED", 1);
                    dc.SetCubeWeight ("SEMIDEPLOYED", 0);
                    dc.SetCubeWeight ("PACKED", 0);
                    dc.SetOcclusionMultiplier (0);
                }
                break;
            }

            EditorUtils.RunOnAllParts (calculateDrag);

            position /= (float)Cd;
            Cd *= PhysicsGlobals.DragMultiplier;
            return position;
        }

        void calculateDrag (Part part)
        {
            if (part.GroundParts ()) {
                return;
            }

            Vector3 cop;
            if (part.GetCoP(out cop)) {
                part.DragCubes.ForceUpdate (true, true);
                part.DragCubes.SetDragWeights ();
                part.DragCubes.SetPartOcclusion ();

                Vector3 direction = part.partTransform.InverseTransformDirection (VelocityDirection);
                part.DragCubes.SetDrag (direction, mach);
                float drag = GetDrag(part);
                position += cop * drag;
                Cd += drag;
            }
        }

        float calculateTerminalVelocity ()
        {
            float mass = RCSBuildAid.ReferenceMarker.GetComponent<MassEditorMarker> ().mass;
            /* the 1000 factor is because mass is in tons */
            return Mathf.Sqrt ((float)((2000 * gravity * mass) / (density * Cd)));
        }

        Vector3 calculateDragForce()
        {
            /* the 1000 factor is because force is in kN */
            float magnitude = (float)(0.0005 * Cd * density * speed * speed);
            return -VelocityDirection * magnitude;
        }
    }
}

