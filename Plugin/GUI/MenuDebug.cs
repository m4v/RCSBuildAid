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
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public class MenuDebug : ToggleableContent
    {
        const string title = "DEBUG";
        DebugVesselTree vesselTreeWindow;
        DebugPartList partListWindow;
        DebugMiscInfo debugMiscInfo;

        protected override string buttonTitle {
            get { return title; }
        }

        protected override void content ()
        {
            MarkerForces comv = RCSBuildAid.VesselForces;
            MomentOfInertia moi = comv.MoI;
            GUILayout.BeginHorizontal (GUI.skin.box);
            {
                GUILayout.BeginVertical (); 
                {
                    GUILayout.Label ("Mouse");
                    GUILayout.Label ("MoI");
                    GUILayout.Label ("Ang Acc");
                    GUILayout.Label ("Ang Acc");
                }
                GUILayout.EndVertical ();
                GUILayout.BeginVertical ();
                {
                    GUILayout.Label (String.Format ("{0}, {1}", Input.mousePosition.x, Input.mousePosition.y));
                    GUILayout.Label (moi.value.ToString("0.## tm²"));
                    float angAcc = comv.Torque().magnitude / moi.value;
                    GUILayout.Label (angAcc.ToString ("0.## r/s²"));
                    GUILayout.Label ((angAcc * Mathf.Rad2Deg).ToString ("0.## °/s²"));
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndHorizontal ();
            DebugSettings.labelMagnitudes = 
                GUILayout.Toggle(DebugSettings.labelMagnitudes, "Show vector magnitudes");
            DebugSettings.inFlightAngularInfo = 
                GUILayout.Toggle(DebugSettings.inFlightAngularInfo, "In flight angular data");
            DebugSettings.inFlightPartInfo = 
                GUILayout.Toggle(DebugSettings.inFlightPartInfo, "In flight vessel tree");
            DebugSettings.startInOrbit = 
                GUILayout.Toggle(DebugSettings.startInOrbit, "Launch in orbit");
            if (GUILayout.Button ("Toggle vessel tree window")) {
                if (vesselTreeWindow == null) {
                    vesselTreeWindow = gameObject.AddComponent<DebugVesselTree> ();
                } else {
                    Destroy (vesselTreeWindow);
                }
            }
            if (GUILayout.Button ("Toggle part list window")) {
                if (partListWindow == null) {
                    partListWindow = gameObject.AddComponent<DebugPartList> ();
                } else {
                    Destroy (partListWindow);
                }
            }
            if (GUILayout.Button ("More info")) {
                if (debugMiscInfo == null) {
                    debugMiscInfo = gameObject.AddComponent<DebugMiscInfo> ();
                } else {
                    Destroy (debugMiscInfo);
                }
            }
        }
    }

    public abstract class DebugWindow : MonoBehaviour
    {
        Rect winRect = new Rect(280, 100, 300, 500);
        Vector2 scrollPosition;
        int winId;

        void Awake ()
        {
            winId = gameObject.GetInstanceID () + DateTime.Now.Millisecond;
        }

        void OnGUI()
        {
            winRect = GUILayout.Window (winId, winRect, drawWindow, title);
        }

        void drawWindow (int id)
        {
            scrollPosition = GUILayout.BeginScrollView (scrollPosition);
            {
                GUILayout.BeginVertical ();
                {
                    drawContent ();
                }
                GUILayout.EndVertical ();
            }
            GUILayout.EndScrollView ();
            GUI.DragWindow ();
        }

        protected void PartInfo(Part part) {
            Vector3 com;
            part.GetCoM (out com);
            GUILayout.Label (string.Format (
                "phy: {0} rb: {1} m: {2:F3} cm: {3:F3}\n" + 
                "com: {4}", 
                part.physicalSignificance,
                part.rb != null,
                part.GetTotalMass(),
                part.GetPhysicslessChildMassInEditor (),
                com));
            var engines = part.FindModulesImplementing<ModuleEngines> ();
            foreach(var engine in engines) {
                GUILayout.Label ("<b>ModuleEngine</b> " + engine.engineID);
                GUILayout.Label (string.Format (
                    "min thrust: {0} max thrust: {1}\n" +
                    "vac isp: {2} asl isp: {3}",
                    engine.minThrust, engine.maxThrust, 
                    engine.atmosphereCurve.Evaluate (0f),
                    engine.atmosphereCurve.Evaluate (1f)));
            }
            var enginesfx = part.FindModulesImplementing<ModuleEnginesFX> ();
            foreach(var engine in enginesfx) {
                GUILayout.Label ("<b>ModuleEngineFX</b> " + engine.engineID);
                GUILayout.Label (string.Format (
                    "min thrust: {0} max thrust: {1}\n" +
                    "vac isp: {2} asl isp: {3}",
                    engine.minThrust, engine.maxThrust, 
                    engine.atmosphereCurve.Evaluate (0f),
                    engine.atmosphereCurve.Evaluate (1f)));
            }
        }

        protected bool Button (string text) {
            return GUILayout.Button (text, MainWindow.style.smallButton, GUILayout.Width (15));
        }

        abstract protected void drawContent ();
        abstract protected string title { get; }
    }

    public class DebugVesselTree : DebugWindow
    {
        Dictionary<int, bool> treebranch = new Dictionary<int, bool> ();
        Dictionary<int, bool> partinfo = new Dictionary<int, bool> ();

        protected override string title {
            get { return "Vessel Parts"; }
        }

        protected override void drawContent ()
        {
            Part rootPart;
            if (FlightGlobals.ready) {
                rootPart = FlightGlobals.ActiveVessel.rootPart;
            } else {
                rootPart = EditorLogic.RootPart;
            }

            if (rootPart == null) {
                return;
            }

            partRecurse (rootPart, 0);
        }

        void partState (int id, out bool open, out bool info) {
            treebranch.TryGetValue (id, out open);
            partinfo.TryGetValue (id, out info);
        }

        void partRecurse (Part part, int nest) {
            bool open, info;
            var id = part.GetInstanceID ();
            partState (id, out open, out info);
            GUILayout.BeginHorizontal ();
            {
                GUILayout.Space (nest * 8);
                if (part.children.Count > 0) {
                    if (Button (open ? "-" : "+")) {
                        treebranch [id] = !open;
                    }
                }
                if (Button ("i")) {
                    partinfo [id] = !info;
                }
                GUILayout.Label (part.partInfo.name);
            }
            GUILayout.EndHorizontal ();
            if (info) {
                PartInfo (part);
            }
            if (open) {
                foreach (Part child in part.children) {
                    partRecurse (child, nest + 1);
                }
            }
        }
    }

    public class DebugPartList : DebugWindow
    {
        Dictionary<int, bool> partinfo = new Dictionary<int, bool> ();

        protected override string title {
            get { return "Part list"; }
        }

        void partState (int id, out bool info) {
            partinfo.TryGetValue (id, out info);
        }

        protected override void drawContent ()
        {
            for (int i = 0; i < PartLoader.LoadedPartsList.Count; i++) {
                var apart = PartLoader.LoadedPartsList [i];
                var part = apart.partPrefab;
                var id = part.GetInstanceID ();
                bool info;
                partState (id, out info);
                GUILayout.BeginHorizontal ();
                {
                    if (Button ("i")) {
                        partinfo [id] = !info;
                    }
                    GUILayout.Label (part.partInfo.name);
                }
                GUILayout.EndHorizontal ();
                if (info) {
                    PartInfo (part);
                }
            }
        }
    }

    public class DebugMiscInfo : DebugWindow
    {
        bool engines;
        #region implemented abstract members of DebugWindow

        protected override void drawContent ()
        {
            var body = Settings.selected_body;
            var pressure = body.GetPressure (0);
            var temp = body.GetTemperature (0);
            GUILayout.Label ("<b>Selected celestial body:</b> " + body.name);
            GUILayout.Label (string.Format (
                "constants:\n" +
                "g: {0:F2} g {5:F2} m/s²\n" +
                "ASL p: {1:F2} kPa {2:F2} atm\n" +
                "ASL den {3:F2} kg/m³\n" +
                "ASL temp {4:F2} K\n" +
                "methods:\n" +
                "ASL p: {6:F2} kPa {7:F2} atm\n" +
                "ASL den {8:F2} kg/m³\n" +
                "ASL temp {9:F2} K",
                body.GeeASL, 
                body.atmospherePressureSeaLevel, body.atmospherePressureSeaLevel * PhysicsGlobals.KpaToAtmospheres,
                body.atmDensityASL, body.atmosphereTemperatureSeaLevel,
                body.ASLGravity (),
                pressure, pressure * PhysicsGlobals.KpaToAtmospheres,
                body.GetDensity (pressure, temp), temp
            ));

            GUILayout.Label ("<b>Vessel forces</b>");
            GUILayout.Label (string.Format (
                "torque {0}\n thrust {1}",
                RCSBuildAid.VesselForces.Torque (),
                RCSBuildAid.VesselForces.Thrust ()
            ));
            GUILayout.Label ("<b>Module forces</b>");
            GUILayout.BeginHorizontal (); {
                if (Button (engines ? "-" : "+")) {
                    engines = !engines;
                }
                GUILayout.Label ("Engine forces count: " + RCSBuildAid.Engines.Count);
            }
            GUILayout.EndHorizontal ();
            if (engines) {
                foreach (var e in RCSBuildAid.Engines) {
                    var f = e.GetComponent<EngineForce> ();
                    if (f == null) {
                        GUILayout.Label (e.part.partInfo.name + " no force module");
                    } else {
                        GUILayout.Label (string.Format (
                            "{0}, vectors:",
                            e.part.partInfo.name
                        ));
                        foreach (var v in f.vectors) {
                            GUILayout.Label (v.value.ToString());
                        }
                    }
                }
            }
            GUILayout.Label ("RCS forces count: " + RCSBuildAid.RCS.Count);
        }

        protected override string title {
            get { return "Debug info"; }
        }

        #endregion
    }
}

