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
        public event Action<PluginMode> ModeChanged;
        public event Action<Direction> DirectionChanged;
        public event Action ConfigSaving;
        public static event Action PluginEnabled;
        public static event Action PluginDisabled;

        public void OnModeChanged ()
        {
            if (ModeChanged != null) {
                ModeChanged(RCSBuildAid.Mode);
            }
        }

        public void OnDirectionChanged ()
        {
            if (DirectionChanged != null) {
                DirectionChanged (RCSBuildAid.Direction);
            }
        }

        public void OnPluginEnabled ()
        {
            if (PluginEnabled != null) {
                PluginEnabled ();
            }
        }

        public void OnPluginDisabled ()
        {
            if (PluginDisabled != null) {
                PluginDisabled ();
            }
        }

        public void RegisterEvents ()
        {
            GameEvents.onGameSceneLoadRequested.Add (onGameSceneChange);
            GameEvents.onEditorPartEvent.Add (onEditorPartEvent);
            GameEvents.onEditorRestart.Add (onEditorRestart);
        }

        public void UnregisterEvents ()
        {
            GameEvents.onGameSceneLoadRequested.Remove (onGameSceneChange);
            GameEvents.onEditorPartEvent.Remove (onEditorPartEvent);
            GameEvents.onEditorRestart.Remove (onEditorRestart);
        }

        void onGameSceneChange(GameScenes scene)
        {
            /* save settings */
            if (ConfigSaving != null) {
                ConfigSaving ();
            }
            Settings.SaveConfig ();
        }

        void onEditorRestart () {
            RCSBuildAid.SetActive (false);
        }

        void onEditorPartEvent (ConstructionEventType evt, Part part)
        {
            switch (evt) {
            case ConstructionEventType.PartDeleted:
                if (part == EditorLogic.RootPart) {
                    RCSBuildAid.SetActive (false);
                }
                break;
            }
        }
    }
}

