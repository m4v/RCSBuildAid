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
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuMarkers : ToggleableContent
    {
        string title = "Markers";
        bool showMenu = false;

        protected override string buttonTitle {
            get { return title; }
        }

        public override bool value {
            get { return showMenu; }
            set { showMenu = value; }
        }

        protected override void update ()
        {
        }

        protected override void content ()
        {
            /* markers toggles */
            GUILayout.BeginVertical ();
            {
                GUILayout.BeginHorizontal ();
                {
                    for (int i = 0; i < 3; i++) {
                        MarkerType marker = (MarkerType)i;
                        bool visibleBefore = RCSBuildAid.isMarkerVisible(marker);
                        bool visibleAfter = GUILayout.Toggle (visibleBefore, marker.ToString());
                        if (visibleBefore != visibleAfter) {
                            RCSBuildAid.setMarkerVisibility(marker, visibleAfter);
                        }
                    }
                }
                GUILayout.EndHorizontal ();
                GUILayout.BeginHorizontal ();
                {
                    GUILayout.Label ("Size", MainWindow.style.sizeLabel);
                    Settings.marker_scale = GUILayout.HorizontalSlider (Settings.marker_scale, 0, 1);
                }
                GUILayout.EndHorizontal ();
            }
            GUILayout.EndVertical ();
        }
    }
}

