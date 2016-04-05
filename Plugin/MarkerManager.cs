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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCSBuildAid
{
    public enum MarkerType { CoM, DCoM, ACoM };

    public class MarkerManager : MonoBehaviour
    {

        static Dictionary<MarkerType, GameObject> referenceDict = new Dictionary<MarkerType, GameObject> ();

        GameObject protoMarker;
        EditorVesselOverlays vesselOverlays;

        public static MarkerManager instance { get; private set; }
        public static GameObject CoM { get; private set; }
        public static GameObject DCoM { get; private set; }
        public static GameObject ACoM { get; private set; }
        public static GameObject CoD { get; private set; }

        public static MassEditorMarker WetMass { get; private set; }
        public static MassEditorMarker DryMass { get; private set; }

        public MarkerManager ()
        {
            instance = this;
        }

        public static GameObject GetMarker (MarkerType comref)
        {
            return referenceDict [comref];
        }

        public static void SetMarkerVisibility (MarkerType marker, bool value)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            if (value) {
                markerVis.Show ();
            } else {
                markerVis.SettingsToggle = false;
            }
            switch (marker) {
            case MarkerType.CoM:
                Settings.show_marker_com = value;
                break;
            case MarkerType.DCoM:
                Settings.show_marker_dcom = value;
                break;
            case MarkerType.ACoM:
                Settings.show_marker_acom = value;
                break;
            }
        }

        public static bool IsMarkerVisible (MarkerType marker)
        {
            GameObject markerObj = referenceDict [marker];
            MarkerVisibility markerVis = markerObj.GetComponent<MarkerVisibility> ();
            return markerVis.isVisible;
        }

        void Awake ()
        {
            // Analysis disable once AccessToStaticMemberViaDerivedType
            vesselOverlays = (EditorVesselOverlays)GameObject.FindObjectOfType (
                typeof(EditorVesselOverlays));
        }

        void Start ()
        {
            initMarkers (); /* must be in Start because CoMmarker is null in Awake */
            RCSBuildAid.SetReferenceMarker(RCSBuildAid.ReferenceType);
            activateMarkers (RCSBuildAid.Enabled);

            Events.PluginToggled += onPluginToggled;
        }

        void OnDestroy ()
        {
            Events.PluginToggled -= onPluginToggled;
        }

        void initMarkers ()
        {
            /* get CoM */
            if (vesselOverlays.CoMmarker == null) {
                throw new Exception ("CoM marker is null, this shouldn't happen.");
            }
            CoM = vesselOverlays.CoMmarker.gameObject;

            protoMarker = (GameObject)UnityEngine.Object.Instantiate (CoM);
            Destroy (protoMarker.GetComponent<EditorMarker_CoM> ()); /* we don't need this */
            protoMarker.name = "Marker Prototype";
            if (protoMarker.transform.childCount > 0) {
                /* Stock CoM doesn't have any attached objects, if there's some it means
                 * there's a plugin doing the same thing as us. We don't want extra
                 * objects */
                for (int i = 0; i < protoMarker.transform.childCount; i++) {
                    Destroy (protoMarker.transform.GetChild (i).gameObject);
                }
            }

            /* init DCoM */
            DCoM = (GameObject)UnityEngine.Object.Instantiate (protoMarker);
            DCoM.name = "DCoM Marker";

            /* init ACoM */
            ACoM = (GameObject)UnityEngine.Object.Instantiate (protoMarker);
            ACoM.name = "ACoM Marker";

            /* init CoD */
            CoD = (GameObject)UnityEngine.Object.Instantiate (protoMarker);

            referenceDict [MarkerType.CoM] = CoM;
            referenceDict [MarkerType.DCoM] = DCoM;
            referenceDict [MarkerType.ACoM] = ACoM;

            /* CoM setup, replace stock component with our own */
            CoMMarker comMarker = CoM.AddComponent<CoMMarker> ();
            comMarker.posMarkerObject = vesselOverlays.CoMmarker.posMarkerObject;
            Destroy (vesselOverlays.CoMmarker);
            vesselOverlays.CoMmarker = comMarker;
            WetMass = comMarker;

            /* setup DCoM */
            DCoMMarker dcomMarker = DCoM.AddComponent<DCoMMarker> (); /* we do need this    */
            dcomMarker.posMarkerObject = DCoM;
            DryMass = dcomMarker;

            /* setup ACoM */
            var acomMarker = ACoM.AddComponent<AverageMarker> ();
            acomMarker.posMarkerObject = ACoM;
            acomMarker.CoM1 = comMarker;
            acomMarker.CoM2 = dcomMarker;

            /* setup CoD */
            var codMarker = CoD.AddComponent<CoDMarker> ();
            codMarker.posMarkerObject = CoD;
            CoD.SetActive (false);

            /* attach our method to the CoM toggle button */
            vesselOverlays.toggleCoMbtn.onClick.AddListener (delegate { comButtonClick (); });

            try {
                /* scaling for CoL and CoT markers */
                vesselOverlays.CoLmarker.gameObject.AddComponent<MarkerScaler> ();
                vesselOverlays.CoTmarker.gameObject.AddComponent<MarkerScaler> ();
            } catch (NullReferenceException) {
                Debug.LogWarning ("CoL/CoT marker is null, this shouldn't happen.");
            }
        }

        void comButtonClick ()
        {
            if (RCSBuildAid.Enabled) {
                /* plugin enabled, CoM button is for toggle marker visibility */
                bool visible = !CoM.GetComponent<MarkerVisibility> ().GeneralToggle;
                CoM.GetComponent<MarkerVisibility> ().GeneralToggle = visible;
                DCoM.GetComponent<MarkerVisibility> ().GeneralToggle = visible;
                ACoM.GetComponent<MarkerVisibility> ().GeneralToggle = visible;
                /* we need the CoM to remain active, but we can't stop the editor from
                 * deactivating it when the CoM toggle button is used, so we toggle it now so is
                 * toggled again by the editor. That way it will remain active. */
                CoM.SetActive(!CoM.activeInHierarchy);
            }

            if (!RCSBuildAid.Enabled) {
                /* restore CoM visibility, so the regular CoM toggle button works. */
                var markerVisibility = CoM.GetComponent<MarkerVisibility> ();
                if (markerVisibility != null) {
                    markerVisibility.Show ();
                }
            }
        }

        void onPluginToggled (bool value, bool byUser)
        {
            activateMarkers (value);
        }

        void activateMarkers(bool value)
        {
            CoM.SetActive (value);
            DCoM.SetActive (value);
            ACoM.SetActive (value);
        }
    }
}

