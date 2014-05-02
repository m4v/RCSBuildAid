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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuResources : ToggleableContent
    {
        string title = "Resources";
        List<DCoMResource> Resources = new List<DCoMResource> ();

        protected override string buttonTitle {
            get { return title; }
        }

        public override bool value {
            get { return Settings.menu_res_mass; }
            set { Settings.menu_res_mass = value; }
        }

        protected override void update ()
        {
            Resources = DCoM_Marker.Resource.Values.OrderByDescending (o => o.mass).ToList ();
        }

        protected override void content ()
        {
            /* resources */
            if (Resources.Count != 0) {
                GUILayout.BeginVertical ();
                {
                    GUILayout.BeginHorizontal ();
                    {
                        GUILayout.BeginVertical ();
                        {
                            GUILayout.Label ("Name", MainWindow.style.resourceTableName);
                            foreach (DCoMResource resource in Resources) {
                                string name = resource.name;
                                if (!resource.isMassless ()) {
                                    Settings.resource_cfg [name] = 
                                        GUILayout.Toggle (Settings.resource_cfg [name], name);
                                } else {
                                    GUILayout.Label (name, MainWindow.style.resourceLabel);
                                }
                            }
                        }
                        GUILayout.EndVertical ();
                        GUILayout.BeginVertical ();
                        {
                            if (GUILayout.Button (Settings.resource_amount ? "Amnt" : "Mass", MainWindow.style.clickLabelGray)) {
                                Settings.resource_amount = !Settings.resource_amount;
                            }
                            foreach (DCoMResource resource in Resources) {
                                string s = String.Empty;
                                if (Settings.resource_amount) {
                                    s = resource.amount.ToString("F0");
                                } else {
                                    if (!resource.isMassless ()) {
                                        s = resource.mass.ToString("0.## t");
                                    }
                                }
                                GUILayout.Label (s);
                            }
                        }
                        GUILayout.EndVertical ();
                    }
                    GUILayout.EndHorizontal ();
                }
                GUILayout.EndVertical ();
            }
        }
    }
}

