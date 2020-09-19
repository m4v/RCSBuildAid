/* Copyright © 2013-2020, Elián Hanisch <lambdae2@gmail.com>
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace RCSBuildAid
{
    public static class PartExtensions
    {
        public static bool GroundParts (this Part part)
        {
            /* Ground parts, stuff that stays in the ground at launch */
            return part.Modules.Contains ("LaunchClamp");
        }

        public static float GetTotalMass (this Part part) {
            return part.mass + part.GetResourceMass ();
        }

        public static float GetDryMass (this Part part) {
            return part.mass;
        }

        public static float GetPhysicslessChildMassInEditor (this Part part) {
            /* in the editor parts aren't setup for part.GetPhysicsLessChildMass () to work. */
            float m = 0f;
            for (int i = 0; i < part.children.Count; i++) {
                Part child = part.children [i];
                if (child.physicalSignificance == Part.PhysicalSignificance.NONE) {
                    m += child.GetTotalMass ();
                }
            }
            return m;
        }

        static Vector3 getCoM (Part part) {
            /* part.WCoM fails in the editor */
            return part.partTransform.position + part.partTransform.rotation * part.CoMOffset;
        }

        static Vector3 getCoP (Part part) {
            return part.partTransform.position + part.partTransform.rotation * part.CoPOffset;
        }

        public static Vector3 GetCoM (this Part part)
        {
            Profiler.BeginSample("[RCSBA] PartExt GetCoM");
            if (part != EditorLogic.RootPart) {
                while (part.physicalSignificance == Part.PhysicalSignificance.NONE) {
                    /* the only part that has no parent is the root, which always has physics.
                     * selected parts only get here when they have a potential parent */

                    // ReSharper disable once Unity.NoNullCoalescing
                    Part parent = part.parent ?? part.potentialParent;
                    Debug.Assert(parent != null, "[RCSBA, PartExtensions]: GetCoM, parent != null");
                    /* find the parent that has physics */
                    part = parent;
                }
            }
            var com = getCoM(part);
            Profiler.EndSample();
            return com;
        }

        public static Vector3 GetCoP (this Part part)
        {
            Profiler.BeginSample("[RCSBA] PartExt GetCoP");
            if (PhysicsGlobals.ApplyDragToNonPhysicsPartsAtParentCoM && part != EditorLogic.RootPart) {
                while (part.physicalSignificance == Part.PhysicalSignificance.NONE) {
                    // ReSharper disable once Unity.NoNullCoalescing
                    Part parent = part.parent ?? part.potentialParent;
                    Debug.Assert(parent != null, "[RCSBA, PartExtensions]: GetCoP, parent != null");
                    /* find the parent that has physics */
                    part = parent;
                }
            }
            var cop = getCoP (part);
            Profiler.EndSample();
            return cop;
        }

        public static float GetSelectedMass (this Part part) {
            Profiler.BeginSample("[RCSBA] PartExt GetSelectedMass");
            float mass = part.GetDryMass ();
            for (int i = 0; i < part.Resources.Count; i++) {
                PartResource res = part.Resources [i];
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (res.info.density == 0) {
                    /* no point in toggling it off/on from the DCoM marker */
                    continue;
                }
                if (Settings.GetResourceCfg (res.info.name, false) || !res.flowState) {
                    /* if resource isn't in the cfg, is a likely a resource added by a mod so default to false */
                    mass += (float)(res.amount * res.info.density);
                }
            }
            Profiler.EndSample();
            return mass;
        }

        public static bool HasModule<T> (this Part part) where T : PartModule
        {
            for (int i = 0; i < part.Modules.Count; i++) {
                var mod = part.Modules [i];
                if (mod is T) {
                    return true;
                }
            }
            return false;
        }

        public static List<PartModule> GetModulesOf<T> (this Part part) where T : PartModule
        {
            var list = new List<PartModule> ();
            for (int i = part.Modules.Count - 1; i >= 0; i--) {
                var mod = part.Modules [i];
                if (mod is T) {
                    list.Add (mod);
                }
            }
            return list;
        }
    }
}
