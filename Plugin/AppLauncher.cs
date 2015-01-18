// /* Copyright © 2013-2014, Elián Hanisch <lambdae2@gmail.com>
//  *
//  * This program is free software: you can redistribute it and/or modify
//  * it under the terms of the GNU Lesser General Public License as published by
//  * the Free Software Foundation, either version 3 of the License, or
//  * (at your option) any later version.
//  *
//  * This program is distributed in the hope that it will be useful,
//  * but WITHOUT ANY WARRANTY; without even the implied warranty of
//  * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  * GNU Lesser General Public License for more details.
//  *
//  * You should have received a copy of the GNU Lesser General Public License
//  * along with this program.  If not, see <http://www.gnu.org/licenses/>.
//  */
//
using UnityEngine;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class AppLauncher : MonoBehaviour
    {
        public static AppLauncher instance;

        static ApplicationLauncherButton button;

        const string iconPath = "RCSBuildAid/Textures/iconAppLauncher";
        ApplicationLauncher.AppScenes visibleScenes = 
            ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;


        void Awake ()
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
        }

        void _removeButton () {
            if (button != null) {
                ApplicationLauncher.Instance.RemoveModApplication (button);
                button = null;
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

        void onTrue ()
        {
            RCSBuildAid.Enabled = true;
        }

        void onFalse ()
        {
            RCSBuildAid.Enabled = false;
        }
    }
}

