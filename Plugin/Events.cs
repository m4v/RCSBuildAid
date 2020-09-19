/* Copyright © 2013-2020, Elián Hanisch <lambdae2@gmail.com>
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
        /* a part event occurred */
        public static event Action PartEvent;
        public static event Action PartDrag;
        public static event Action PodPicked;
        public static event Action PodDeleted;

        /*
         * Methods for activate plugin events
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
            Debug.Log($"[RCSBA EVENT]: Plugin enabled set to {value}, user {byUser}");
#endif
            PluginToggled?.Invoke (value, byUser);
        }
        
        /*
         * Convenience methods for handle game events.
         */
        
        void OnVesselPartChanged()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Vessel part change");
#endif
            VesselPartChanged?.Invoke();
        }

        void OnSelectionChanged()
        {
#if DEBUG
            Debug.Log("[RCSBA EVENT]: Part selection change");
#endif
            SelectionChanged?.Invoke();
        }

        /*
         * Game events used to activate RCSBA's events.
         */
        
        public void HookEvents ()
        {
            /* don't add static methods, GameEvents doesn't like that. */
            GameEvents.onGameSceneLoadRequested.Add (onGameSceneChange);
            GameEvents.onEditorPartEvent.Add (onEditorPartEvent);
            GameEvents.onEditorScreenChange.Add (onEditorScreenChange);
            GameEvents.onEditorShipModified.Add(onEditorShipModified);
            GameEvents.onEditorStarted.Add(onEditorStarted);
            GameEvents.onEditorUndo.Add(onEditorUndoRedo);
            GameEvents.onEditorRedo.Add(onEditorUndoRedo);
            GameEvents.onEditorPodPicked.Add(onEditorPodPicked);
            GameEvents.onEditorPodDeleted.Add(onEditorPodDeleted);
        }

        public void UnhookEvents ()
        {
            GameEvents.onGameSceneLoadRequested.Remove (onGameSceneChange);
            GameEvents.onEditorPartEvent.Remove (onEditorPartEvent);
            GameEvents.onEditorScreenChange.Remove (onEditorScreenChange);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            GameEvents.onEditorStarted.Remove(onEditorStarted);
            GameEvents.onEditorUndo.Remove(onEditorUndoRedo);
            GameEvents.onEditorRedo.Remove(onEditorUndoRedo);
            GameEvents.onEditorPodPicked.Remove(onEditorPodPicked);
            GameEvents.onEditorPodDeleted.Remove(onEditorPodDeleted);
        }

        void onEditorStarted()
        {
#if DEBUG
            Debug.Log("[RCSBA]: Editor started");
#endif
            EditorStart?.Invoke();
            OnVesselPartChanged();
        }
        
        void onGameSceneChange(GameScenes scene)
        {
#if DEBUG
            Debug.Log($"[RCSBA EVENT]: Leaving the editor");
#endif
            LeavingEditor?.Invoke();
        }
        
        void onEditorScreenChange (EditorScreen screen)
        {
            EditorScreenChanged?.Invoke (screen);
        }

        void onEditorShipModified(ShipConstruct ship)
        {
#if DEBUG
            Debug.Log("[RCSBA]: Ship Modified");
#endif
            ShipModified?.Invoke();
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
            PodPicked?.Invoke();
            OnVesselPartChanged();
        }

        void onEditorPodDeleted()
        {
#if DEBUG
            Debug.Log("[RCSBA]: Pod deleted");
#endif
            PodDeleted?.Invoke();
            OnVesselPartChanged();
        }

        void onEditorPartEvent (ConstructionEventType evt, Part part)
        {
#if DEBUG
            switch (evt) { 
            case ConstructionEventType.PartDragging:
            case ConstructionEventType.PartOffsetting:
            case ConstructionEventType.PartRotating:
                /* these events are too noisy for logging */
                break;
            default:
                Debug.Log($"[RCSBA]: Editor Part Event {evt} {part.partInfo.name}");
                break;
            }
#endif
            PartEvent?.Invoke();
            switch (evt) {
            case ConstructionEventType.PartCopied:
            case ConstructionEventType.PartCreated:
                OnSelectionChanged();
                break;
            case ConstructionEventType.PartPicked:
                if (part != EditorLogic.RootPart) {
                    OnSelectionChanged();
                }
                break;
            case ConstructionEventType.PartDropped:
                if (part != EditorLogic.RootPart) {
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
                PartDrag?.Invoke();
                break;
            }
        }
    }
}

