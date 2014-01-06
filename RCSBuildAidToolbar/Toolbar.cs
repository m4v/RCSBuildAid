using System;
using UnityEngine;
using Toolbar;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class Toolbar : MonoBehaviour
    {
        IButton button;

        void Awake ()
        {
            /* first, because RCSBuildAid.Enabled depends of this value */
            RCSBuildAid.toolbarEnabled = true;

            button = ToolbarManager.Instance.add ("RCSBuildAid", "mainButton");
            button.ToolTip = "RCS Build Aid";
            button.OnClick += togglePlugin;
            button.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH);
            setTexture(RCSBuildAid.Enabled);
        }

        void setTexture (bool value)
        {
            if (value) {
                button.TexturePath = "RCSBuildAid/Textures/iconToolbar_active";
            } else {
                button.TexturePath = "RCSBuildAid/Textures/iconToolbar";
            }
        }

        void togglePlugin (ClickEvent evnt)
        {
            bool enabled = !RCSBuildAid.Enabled;
            RCSBuildAid.Enabled = enabled;
            setTexture(enabled);
        }

        void OnDestroy()
        {
            button.Destroy();
        }
    }
}

