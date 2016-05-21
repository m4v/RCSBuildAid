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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class AverageMarker : MassEditorMarker
    {
        public MassEditorMarker CoM1;
        public MassEditorMarker CoM2;

        protected override void Awake ()
        {
            base.Awake();
            scaler.scale = 0.6f;
            var color = XKCDColors.Orange;
            color.a = 0.5f;
            gameObject.GetComponent<Renderer> ().material.color = color;
            gameObject.GetComponent<MarkerVisibility> ().SettingsToggle = Settings.show_marker_acom;
        }

        protected override Vector3 UpdatePosition ()
        {
            Vector3 position = (CoM1.transform.position + CoM2.transform.position) / 2;
            totalMass = (CoM1.mass + CoM2.mass) / 2;
            return position;
        }

        protected override void calculateCoM (Part part)
        {
        }
    }
}
