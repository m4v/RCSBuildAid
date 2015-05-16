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


using UnityEngine;
using Toolbar;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class ToolbarCheck : MonoBehaviour
    {
        ToolbarCheck ()
        {
            Settings.toolbar_plugin_loaded = true;
        }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class Toolbar : MonoBehaviour
    {
        IButton button;
        const string iconActivePath = "RCSBuildAid/Textures/iconToolbar_active";
        const string iconPath = "RCSBuildAid/Textures/iconToolbar";

        void Awake ()
        {
            if (Settings.toolbar_plugin) {
                addButton ();
            }
            Settings.toolbarSetup = toolbarToggle;
        }

        void addButton () {
            button = ToolbarManager.Instance.add ("RCSBuildAid", "mainButton");
            button.ToolTip = "RCS Build Aid";
            button.OnClick += togglePlugin;
            button.Visibility = new GameScenesVisibility(GameScenes.EDITOR);
            setTexture(RCSBuildAid.Enabled);
            Events.PluginEnabled += onPluginEnable;
            Events.PluginDisabled += onPluginDisable;
        }

        void setTexture (bool value)
        {
            button.TexturePath = value ? iconActivePath : iconPath;
        }

        void togglePlugin (ClickEvent evnt)
        {
            bool b = !RCSBuildAid.Enabled;
            RCSBuildAid.SetActive(b);
            setTexture(b);
        }

        void toolbarToggle() {
            if (Settings.toolbar_plugin) {
                addButton ();
            } else {
                removeButton ();
            }
        }

        void removeButton()
        {
            if (button != null) {
                button.Destroy ();
            }
            Events.PluginEnabled -= onPluginEnable;
            Events.PluginDisabled -= onPluginDisable;
        }

        void OnDestroy()
        {
            removeButton ();
        }

        void onPluginEnable()
        {
            setTexture (true);
        }

        void onPluginDisable()
        {
            setTexture (false);
        }
    }
}

