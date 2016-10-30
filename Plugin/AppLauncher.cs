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

using KSP.UI.Screens;

namespace RCSBuildAid
{
    public class AppLauncher
    {
        public static AppLauncher instance;

        static ApplicationLauncherButton button;

        const string iconPath = "RCSBuildAid/Textures/iconAppLauncher";
        const ApplicationLauncher.AppScenes visibleScenes = 
            ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;


        public AppLauncher ()
        {
            if (instance == null) {
                instance = this;

                if (!Settings.toolbar_plugin_loaded) {
                    Settings.applauncher = true;
                }

                if (Settings.applauncher) {
                    addButton ();
                }
            }
        }

        public void addButton () {
            if (ApplicationLauncher.Ready) {
                _addButton ();
            }
            GameEvents.onGUIApplicationLauncherReady.Add(_addButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add (_removeButton);
        }

        public void removeButton () {
            if (ApplicationLauncher.Ready) {
                _removeButton ();
            }
            GameEvents.onGUIApplicationLauncherReady.Remove(_addButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(_removeButton);
        }

        void _addButton(){
            if (button != null) {
                return;
            }
            button = ApplicationLauncher.Instance.AddModApplication (onTrue, onFalse, null, null,
                null, null, visibleScenes, GameDatabase.Instance.GetTexture(iconPath, false));
            if (RCSBuildAid.Enabled) {
                button.SetTrue (false);
            }
            Events.PluginEnabled += onPluginEnable;
            Events.PluginDisabled += onPluginDisable;
        }

        void _removeButton () {
            if (button != null) {
                ApplicationLauncher.Instance.RemoveModApplication (button);
                button = null;
                Events.PluginEnabled -= onPluginEnable;
                Events.PluginDisabled -= onPluginDisable;
            }
        }

        void _removeButton(GameScenes scene)
        {
            _removeButton ();
        }

        void onTrue ()
        {
            RCSBuildAid.SetActive (true);
        }

        void onFalse ()
        {
            RCSBuildAid.SetActive (false);
        }

        void onPluginEnable(bool byUser) {
            if (byUser) {
                button.SetTrue (false);
            }
        }

        void onPluginDisable(bool byUser) {
            if (byUser) {
                button.SetFalse (false);
            }
        }
    }
}

