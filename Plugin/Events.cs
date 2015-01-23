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
        PluginMode previousMode = PluginMode.RCS;
        Direction previousDirection = Direction.right;

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

        public void SetMode (PluginMode new_mode)
        {
            switch(mode) {
            case PluginMode.RCS:
            case PluginMode.Attitude:
            case PluginMode.Engine:
                /* for guesssing which mode to enable when using shortcuts (if needed) */
                previousMode = mode;
                break;
            case PluginMode.none:
                break;
            default:
                /* invalid mode loaded from settings.cfg */
                new_mode = PluginMode.none;
                break;
            }

            switch (new_mode) {
            case PluginMode.Engine:
                /* reset gimbals if we're switching to engines */
                SetDirection (Direction.none);
                break;
            case PluginMode.Attitude:
            case PluginMode.RCS:
                /* these modes should always have a direction */
                SetDirection (previousDirection);
                break;
            }

            mode = new_mode;
            OnModeChange();
        }

        public void SetDirection (Direction new_direction)
        {
            if (direction == new_direction) {
                return;
            }
            if (direction != Direction.none) {
                previousDirection = direction;
            }
            direction = new_direction;
            OnDirectionChange ();
        }

        public void SetPreviousMode ()
        {
            SetMode (previousMode);
        }

    }
}

