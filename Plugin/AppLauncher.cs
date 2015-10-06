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
            } else {
                GameEvents.onGUIApplicationLauncherReady.Add(onAppLauncherReadyAddButton);
            }
        }

        public void removeButton () {
            if (ApplicationLauncher.Ready) {
                _removeButton ();
            } else {
                GameEvents.onGUIApplicationLauncherReady.Add(onAppLauncherReadyRemoveButton);
            }
        }

        void onAppLauncherReadyAddButton ()
        {
            _addButton ();
            GameEvents.onGUIApplicationLauncherReady.Remove (onAppLauncherReadyAddButton);
        }

        void onAppLauncherReadyRemoveButton ()
        {
            _removeButton ();
            GameEvents.onGUIApplicationLauncherReady.Remove (onAppLauncherReadyRemoveButton);
        }

        void _addButton(){
            if (button != null) {
                return;
            }
            button = ApplicationLauncher.Instance.AddModApplication (onTrue, onFalse, null, null,
                null, null, visibleScenes, GameDatabase.Instance.GetTexture(iconPath, false));
            if (RCSBuildAid.Enabled) {
                /* this doesn't seem to work */
                //button.SetTrue (false);
                button.toggleButton.startTrue = true;
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

        void onTrue ()
        {
            RCSBuildAid.SetActive (true);
        }

        void onFalse ()
        {
            RCSBuildAid.SetActive (false);
        }

        void onPluginEnable() {
            button.SetTrue (false);
        }

        void onPluginDisable() {
            button.SetFalse (false);
        }
    }
}

