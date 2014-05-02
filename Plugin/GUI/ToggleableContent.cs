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
    public abstract class ToggleableContent : MonoBehaviour
    {
        abstract public bool value { get; set; }
        abstract protected string buttonTitle { get; }

        /* Draw GUI stuff here */
        public void DrawContent ()
        {
            value = GUILayout.Toggle (value, buttonTitle, MainWindow.style.mainButton);
            if (value) {
                content ();
            }
        }

        /* Calculate stuff outside of the GUI calls here */
        void Update ()
        {
            if (value) {
                update ();
            }
        }

        abstract protected void update ();
        abstract protected void content ();
    }
}

