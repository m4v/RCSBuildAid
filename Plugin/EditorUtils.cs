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

namespace RCSBuildAid
{
    public static class EditorUtils
    {
        static List<PartModule> tempList;
        static Type partModuleType;

        public static List<PartModule> GetModulesOf<T> () where T : PartModule
        {
            tempList = new List<PartModule> ();
            partModuleType = typeof(T);
            RunOnAllParts (findModules);
            return tempList;
        }

        static void findModules (Part part)
        {
            /* check if this part has a module of type T */
            for (int i = 0; i < part.Modules.Count; i++) {
                var mod = part.Modules [i];
                var modType = mod.GetType ();
                if ((modType == partModuleType) || modType.IsSubclassOf(partModuleType)) {
                    tempList.Add (mod);
                    break;
                }
            }
        }
            
        public static void RunOnAllParts (Action<Part> f)
        {
            if (EditorLogic.RootPart == null) {
                return;
            }
            /* run in vessel's parts */
            var parts = EditorLogic.fetch.ship.parts;
            for (int i = 0; i < parts.Count; i++) {
                f (parts[i]);
            }

            /* run in selected parts that are connected */
            if (EditorLogic.SelectedPart != null) {
                Part part = EditorLogic.SelectedPart;
                if (!EditorLogic.fetch.ship.Contains (part) && (part.potentialParent != null)) {
                    recursePart (part, f);

                    for (int i = 0; i < part.symmetryCounterparts.Count; i++) {
                        recursePart(part.symmetryCounterparts [i], f);
                    }
                }
            }
        }

        static void recursePart (Part part, Action<Part> f)
        {
            f (part);
            for (int i = 0; i < part.children.Count; i++) {
                recursePart (part.children [i], f);
            }
        }
    }
}

