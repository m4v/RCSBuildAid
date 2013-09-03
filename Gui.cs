using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class Window : MonoBehaviour
    {
        Rect winPos;
        int winID;
        int winWidth = 100, winHeight = 50;
        string title = "RCSBuildAid";

        enum WinState { RCS, Engine, DCoM, Count };
        enum Reference { CoM, DCoM, Count };
        enum RCSMode { TRANSLATION, ROTATION, Count };

        WinState state;
        Reference reference;
        RCSMode rcsMode;

        delegate void drawMenuDelegate ();
        Dictionary<WinState, drawMenuDelegate> Menus;

        void Awake ()
        {
            winID = gameObject.GetInstanceID ();
            winPos = new Rect (300, 200, winWidth, winHeight);
            Menus = new Dictionary<WinState, drawMenuDelegate>();
            Menus[WinState.RCS] = drawRCSMenu;
            Menus[WinState.Engine] = drawEngineMenu;
            Menus[WinState.DCoM] = drawDCoMMenu;

            state = WinState.RCS;
            reference = Reference.CoM;
            rcsMode = RCSMode.TRANSLATION;
        }

        void OnGUI ()
        {
            winPos = GUILayout.Window (winID, winPos, drawWindow, title);
        }

        void drawWindow (int ID)
        {
            /* Main button bar */
            GUILayout.BeginHorizontal();
            for (int i = 0; i < (int)WinState.Count; i++) {
                if (GUILayout.Toggle((int)state == i, ((WinState)i).ToString(), GUI.skin.button)) {
                    state = (WinState)i;
                }
            }
            GUILayout.EndHorizontal();

            Menus[state]();
            GUI.DragWindow();
        }

        void drawRCSMenu ()
        {
            GUILayout.Label ("Turn rate: 100.00");
            drawRefButton();
            if (GUILayout.Button ("Mode: " + rcsMode)) {
                int m = (int)rcsMode + 1;
                if (m == (int)RCSMode.Count) {
                    m = 0;
                }
                rcsMode = (RCSMode)m;
            }
        }

        void drawEngineMenu ()
        {
            GUILayout.Label ("Turn rate: 100.00");
            drawRefButton();
        }

        void drawDCoMMenu ()
        {
            GUILayout.Toggle(true, "Monopropellant");
            GUILayout.Toggle(true, "Fuel + Oxidizer");
            GUILayout.Toggle(true, "Other");
        }

        void drawRefButton ()
        {
            if (GUILayout.Button ("Reference: " + reference)) {
                int i = (int)reference + 1;
                if (i == (int)Reference.Count) {
                    i = 0;
                }
                reference = (Reference)i;
            }
        }
    }
}

