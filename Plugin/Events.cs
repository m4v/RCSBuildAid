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
using KSP.UI.Screens;
using UnityEngine;

namespace RCSBuildAid
{
    public class Events
    {
        
        /*
         * Events used by RCSBA objects
         */
        
        public static event Action<PluginMode> ModeChanged;
        public static event Action<Direction> DirectionChanged;
        public static event Action BeforeConfigSave;
        public static event Action<bool> PluginEnabled;
        public static event Action<bool> PluginDisabled;
        public static event Action<bool, bool> PluginToggled;
        public static event Action EditorStart;
        public static event Action LeavingEditor;
        /* Editor screen (crew, action groups, etc) changed */
        public static event Action<EditorScreen> EditorScreenChanged;
        /* vessel got a part de/attached */
        public static event Action VesselPartChanged;
        public static event Action ShipModified;
        /* picked or dropped a part */
        public static event Action SelectionChanged;
        public static event Action<Part> PartCreated;
        /* a part event occurred */
        public static event Action PartEvent;
        public static event Action<Part> PartPicked;
        public static event Action<Part> PartDropped;
        public static event Action PartDrag;
        public static event Action RootPartPicked;
        public static event Action RootPartDropped;
        public static event Action PodPicked;
        public static event Action PodDeleted;

        /*
         * Event activate methods
         */
        
        public static void OnBeforeConfigSave()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Saving configuration");
#endif
            BeforeConfigSave?.Invoke();
        }
        
        public static void OnModeChanged ()
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Mode changed to {RCSBuildAid.Mode}");
#endif
            ModeChanged?.Invoke(RCSBuildAid.Mode);
        }

        public static void OnDirectionChanged ()
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Direction changed to {RCSBuildAid.Direction}");
#endif
            DirectionChanged?.Invoke (RCSBuildAid.Direction);
        }

        public static void OnPluginEnabled (bool byUser)
        {
            PluginEnabled?.Invoke (byUser);
        }

        public static void OnPluginDisabled (bool byUser)
        {
            PluginDisabled?.Invoke (byUser);
        }

        public static void OnPluginToggled (bool value, bool byUser)
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Plugin toggled to {value}");
#endif
            PluginToggled?.Invoke (value, byUser);
        }

        public static void OnLeavingEditor ()
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Leaving the editor");
#endif
            LeavingEditor?.Invoke();
        }

        public static void OnPartCreated(Part part)
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Part created {part.name}");
#endif
            PartCreated?.Invoke (part);
        }

        public static void OnPartChanged ()
        {
            PartEvent?.Invoke ();
        }

        public static void OnVesselPartChanged()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Vessel part change");
#endif
            VesselPartChanged?.Invoke();
        }

        public static void OnShipModified()
        {
            ShipModified?.Invoke();
        }

        public static void OnSelectionChanged()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Part selection change");
#endif
            SelectionChanged?.Invoke();
        }

        public static void OnPartPicked(Part part)
        {
            PartPicked?.Invoke(part);
        }

        public static void OnPartDropped(Part part)
        {
            PartDropped?.Invoke(part);
        }

        public static void OnPartDrag()
        {
            PartDrag?.Invoke();
        }

        public static void OnRootPartPicked ()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Root part picked");
#endif
            RootPartPicked?.Invoke();
        }

        public static void OnRootPartDropped()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Root part dropped");
#endif
            RootPartDropped?.Invoke();
        }

        public static void OnEditorScreenChanged (EditorScreen screen)
        {
            EditorScreenChanged?.Invoke (screen);
        }

        public static void OnEditorPodPicked()
        {
            PodPicked?.Invoke();
        }

        public static void OnEditorPodDeleted()
        {
            PodDeleted?.Invoke();
        }
        
        /*
         * Game events used to activate RCSBA's events. Only RCSBuildAid.Events class uses them.
         */
        
        public void HookEvents ()
        {
            /* don't add static methods, GameEvents doesn't like that. */
            GameEvents.onGameSceneLoadRequested.Add (onGameSceneChange);
            GameEvents.onEditorPartEvent.Add (onEditorPartEvent);
            GameEvents.onEditorRestart.Add (onEditorRestart);
            GameEvents.onEditorScreenChange.Add (onEditorScreenChange);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onEditorVariantApplied.Add(onEditorVariantApplied);
            GameEvents.onEditorStarted.Add(onEditorStarted);
            GameEvents.onEditorLoad.Add(onEditorLoad);
            GameEvents.onEditorUndo.Add(onEditorUndoRedo);
            GameEvents.onEditorRedo.Add(onEditorUndoRedo);
            GameEvents.onEditorPodPicked.Add(onEditorPodPicked);
            GameEvents.onEditorPodDeleted.Add(onEditorPodDeleted);
        }

        public void UnhookEvents ()
        {
            GameEvents.onGameSceneLoadRequested.Remove (onGameSceneChange);
            GameEvents.onEditorPartEvent.Remove (onEditorPartEvent);
            GameEvents.onEditorRestart.Remove (onEditorRestart);
            GameEvents.onEditorScreenChange.Remove (onEditorScreenChange);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onEditorVariantApplied.Remove(onEditorVariantApplied);
            GameEvents.onEditorStarted.Remove(onEditorStarted);
            GameEvents.onEditorLoad.Remove(onEditorLoad);
            GameEvents.onEditorUndo.Remove(onEditorUndoRedo);
            GameEvents.onEditorRedo.Remove(onEditorUndoRedo);
            GameEvents.onEditorPodPicked.Remove(onEditorPodPicked);
            GameEvents.onEditorPodDeleted.Remove(onEditorPodDeleted);
        }

        void onEditorLoad(ShipConstruct ship, CraftBrowserDialog.LoadType loadType)
        {
#if DEBUG
            Debug.Log("[RCSBA]: Ship loaded");
#endif
            OnVesselPartChanged();
        }

        void onEditorStarted()
        {
#if DEBUG
            Debug.Log("[RCSBA]: Editor started");
#endif
            EditorStart?.Invoke();
            OnVesselPartChanged();
        }
        
        void onEditorRestart () {
#if DEBUG
            Debug.Log("[RCSBA]: Editor restart");
#endif
        }

        void onGameSceneChange(GameScenes scene)
        {
            OnLeavingEditor ();
        }
        
        void onEditorScreenChange (EditorScreen screen)
        {
            OnEditorScreenChanged (screen);
        }

        void onEditorVariantApplied(Part part, PartVariant partVariant)
        {
#if DEBUG
            Debug.Log("[RCSBA]: Variant Applied");
#endif
        }

        void onEditorShipModified(ShipConstruct ship)
        {
#if DEBUG
            Debug.Log("[RCSBA]: Ship Modified");
#endif
            OnShipModified();
        }

        void onEditorUndoRedo(ShipConstruct ship)
        {
            OnVesselPartChanged();
        }

        void onEditorPodPicked(Part part)
        {
#if DEBUG
            Debug.Log("[RCSBA]: Pod picked");
#endif
            OnEditorPodPicked();
            OnVesselPartChanged();
        }

        void onEditorPodDeleted()
        {
#if DEBUG
            Debug.Log("[RCSBA]: Pod deleted");
#endif
            OnEditorPodDeleted();
            OnVesselPartChanged();
        }

        void onEditorPartEvent (ConstructionEventType evt, Part part)
        {
#if DEBUG
            switch (evt) {
            case ConstructionEventType.PartDragging:
            case ConstructionEventType.PartOffsetting:
            case ConstructionEventType.PartRotating:
                break;
            default:
                Debug.Log($"[RCSBA]: Editor Part Event {evt} {part.partInfo.name}");
                break;
            }
#endif
            OnPartChanged ();
            switch (evt) {
            case ConstructionEventType.PartCopied:
            case ConstructionEventType.PartCreated:
                OnPartCreated (part);
                OnSelectionChanged();
                break;
            case ConstructionEventType.PartPicked:
                OnPartPicked(part);
                if (part == EditorLogic.RootPart) {
                    OnRootPartPicked ();
                } else {
                    OnSelectionChanged();
                }
                break;
            case ConstructionEventType.PartDropped:
                OnPartDropped(part);
                if (part == EditorLogic.RootPart) {
                    OnRootPartDropped ();
                } else {
                    OnSelectionChanged();
                }
                break;
            case ConstructionEventType.PartDeleted:
                OnSelectionChanged();
                break;
            case ConstructionEventType.PartAttached:
            case ConstructionEventType.PartDetached:
                OnVesselPartChanged();
                OnSelectionChanged();
                break;
            case ConstructionEventType.PartDragging:
                OnPartDrag();
                break;
            }
        }
    }
}

