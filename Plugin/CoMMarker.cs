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

        protected override Vector3 UpdatePosition ()
        {
            Profiler.BeginSample("[RCSBA] CoM UpdatePosition");
            /* may be required by stock game */
            CraftCoM = base.UpdatePosition ();
            Profiler.EndSample();
            return CraftCoM;
        }

        protected override void calculateCoM (Part part)
        {
            Profiler.BeginSample("[RCSBA] CoM calculateCoM");
            if (part.GroundParts ()) {
                Profiler.EndSample();
                return;
            }
            
            Vector3 com = part.GetCoM();
            float m = part.GetTotalMass ();
            vectorSum += com * m;
            totalMass += m;
            Profiler.EndSample();
        }
    }
}
