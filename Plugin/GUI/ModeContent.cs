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
    public abstract class ModeContent : MonoBehaviour
    {
        abstract protected PluginMode workingMode { get; }
        abstract protected void DrawContent ();

        public void onModeChange (PluginMode mode)
        {
            MainWindow.onDrawModeContent -= DrawContent;
            if (mode == workingMode) {
                MainWindow.onDrawModeContent += DrawContent;
            }
        }
    }
}

