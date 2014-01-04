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
            button.TexturePath = "RCSBuildAid/Textures/iconToolbar";
            button.ToolTip = "RCS Build Aid";
            button.OnClick += (e) => { RCSBuildAid.Enabled = !RCSBuildAid.Enabled; };
            RCSBuildAid.toolbarEnabled = true;
        }

        void OnDestroy()
        {
            button.Destroy();
        }
    }
}

