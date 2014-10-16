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
using System;
using System.IO;
using UnityEngine;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.MainMenu, false)]
    public class AppLauncher : MonoBehaviour
    {
        public static AppLauncher instance;

        string normalIconPath = "GameData/RCSBuildAid/Textures/iconAppLauncher.png";
        string activeIconPath = "GameData/RCSBuildAid/Textures/iconAppLauncher_active.png";

        Texture2D normalIcon = new Texture2D(38, 38);
        Texture2D activeIcon = new Texture2D(38, 38);
        ApplicationLauncher.AppScenes visibleScenes = 
            ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;

        ApplicationLauncherButton button;

        void Awake ()
        {
            if (instance == null) {
                instance = this;
                normalIcon.LoadImage (File.ReadAllBytes (Path.Combine (
                    KSPUtil.ApplicationRootPath, normalIconPath)));
                activeIcon.LoadImage (File.ReadAllBytes (Path.Combine (
                    KSPUtil.ApplicationRootPath, activeIconPath)));

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
                null, null, visibleScenes, normalIcon);
            if (RCSBuildAid.Enabled) {
                button.SetTrue (false);
                button.SetTexture (activeIcon);
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
            button.SetTexture (activeIcon);
        }

        void onFalse ()
        {
            RCSBuildAid.Enabled = false;
            button.SetTexture (normalIcon);
        }
    }
}

