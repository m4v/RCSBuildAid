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
            button = ToolbarManager.Instance.add ("RCSBuildAid", "mainButton");
            button.ToolTip = "RCS Build Aid";
            button.OnClick += togglePlugin;
            setTexture(false);

            RCSBuildAid.toolbarEnabled = true;
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

