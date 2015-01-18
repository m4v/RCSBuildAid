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

namespace RCSBuildAid
{
    public class Events
    {
        PluginMode lastMode = PluginMode.RCS;

        public event Action<PluginMode> onModeChange;
        public event Action<Direction> onDirectionChange;
        public event Action onSave;

        public Events ()
        {
            GameEvents.onGameSceneLoadRequested.Add (OnGameSceneChange);
        }

        public PluginMode mode {
            get { return Settings.plugin_mode; }
            private set { Settings.plugin_mode = value; }
        }

        public Direction direction { 
            get { return Settings.direction; }
            private set { Settings.direction = value; }
        }

        void OnModeChange ()
        {
            if (onModeChange != null) {
                onModeChange(mode);
            }
        }

        void OnDirectionChange ()
        {
            if (onDirectionChange != null) {
                onDirectionChange (direction);
            }
        }

        void OnGameSceneChange(GameScenes scene)
        {
            /* save settings */
            if (onSave != null) {
                onSave ();
            }
            Settings.SaveConfig ();
            GameEvents.onGameSceneLoadRequested.Remove (OnGameSceneChange);
        }

        public void SetMode (PluginMode mode)
        {
            switch(this.mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
                /* need to remember this so I can know if I was using attitude or translation mode */
                lastMode = this.mode;
                break;
            case PluginMode.Engine:
            case PluginMode.none:
                break;
            default:
                /* invalid mode */
                mode = PluginMode.none;
                break;
            }

            this.mode = mode;
            OnModeChange();
        }

        public void SetDirection (Direction direction)
        {
            if (this.direction == direction) {
                return;
            }
            this.direction = direction;
            OnDirectionChange ();
        }

        public void SetPreviousMode ()
        {
            SetMode (lastMode);
        }

    }
}

