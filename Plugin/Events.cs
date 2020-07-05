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
        public static event Action<PluginMode> ModeChanged;
        public static event Action<Direction> DirectionChanged;
        public static event Action ConfigSaving;
        public static event Action<bool> PluginEnabled;
        public static event Action<bool> PluginDisabled;
        public static event Action<bool, bool> PluginToggled;
        public static event Action LeavingEditor;
        public static event Action<Part> PartCreated;
        public static event Action PartChanged;
        public static event Action VesselPartChanged;
        public static event Action ShipModified;
        public static event Action SelectionChanged;
        public static event Action<Part> PartPicked;
        public static event Action<Part> PartDropped;
        public static event Action PartDrag;
        public static event Action RootPartPicked;
        public static event Action RootPartDropped;
        public static event Action<EditorScreen> EditorScreenChanged;

        public static void OnModeChanged ()
        {
            if (ModeChanged != null) {
                ModeChanged(RCSBuildAid.Mode);
            }
        }

        public static void OnDirectionChanged ()
        {
            if (DirectionChanged != null) {
                DirectionChanged (RCSBuildAid.Direction);
            }
        }

        public static void OnPluginEnabled (bool byUser)
        {
            if (PluginEnabled != null) {
                PluginEnabled (byUser);
            }
        }

        public static void OnPluginDisabled (bool byUser)
        {
            if (PluginDisabled != null) {
                PluginDisabled (byUser);
            }
        }

        public static void OnPluginToggled (bool value, bool byUser)
        {
            if (PluginToggled != null) {
                PluginToggled (value, byUser);
            }
        }

        public static void OnLeavingEditor ()
        {
            if (LeavingEditor != null) {
                LeavingEditor ();
            }
        }

        public static void OnPartCreated(Part part)
        {
            if (PartCreated != null) {
                PartCreated (part);
            }
        }

        public static void OnPartChanged ()
        {
            if (PartChanged != null) {
                PartChanged ();
            }
        }
        
        public static void OnVesselPartChanged()
        {
            VesselPartChanged?.Invoke();
        }

        public static void OnShipModified()
        {
            ShipModified?.Invoke();
        }

        public static void OnSelectionChanged()
        {
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
            if (RootPartPicked != null) {
                RootPartPicked ();
            }
        }

        public static void OnRootPartDropped ()
        {
            if (RootPartDropped != null) {
                RootPartDropped ();
            }
        }

        public static void OnEditorScreenChanged (EditorScreen screen)
        {
            if (EditorScreenChanged != null) {
                EditorScreenChanged (screen);
            }
        }

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
            
            OnVesselPartChanged();
        }

        void onEditorVariantApplied(Part part, PartVariant partVariant)
        {
            #if DEBUG
            Debug.Log ("[RCSBA]: Variant Applied");
            #endif
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            #if DEBUG
            Debug.Log ("[RCSBA]: Ship Modified");
            #endif
            
            OnShipModified();
        }

        void onEditorUndoRedo(ShipConstruct ship)
        {
            OnVesselPartChanged();
        }

        void onGameSceneChange(GameScenes scene)
        {
            OnLeavingEditor ();
            /* save settings */
            if (ConfigSaving != null) {
                ConfigSaving ();
            }
        }

        void onEditorRestart () {
            #if DEBUG
            Debug.Log("[RCSBA]: Editor restart");
            #endif
            
            RCSBuildAid.SetActive (false);
        }

        void onEditorScreenChange (EditorScreen screen)
        {
            OnEditorScreenChanged (screen);
        }

        void onEditorPartEvent (ConstructionEventType evt, Part part)
        {
            #if DEBUG
            if (evt != ConstructionEventType.PartDragging) {
                Debug.Log($"[RCSBA]: Editor Part Event {evt} {part.partInfo.name}");
            }
            #endif
            
            OnPartChanged ();
            switch (evt) {
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
                if (part == EditorLogic.RootPart) {
                    RCSBuildAid.SetActive (false);
                }
                break;
            case ConstructionEventType.PartAttached:
            case ConstructionEventType.PartDetached:
                OnVesselPartChanged();
                OnSelectionChanged();
                break;
            case ConstructionEventType.PartDragging:
                OnPartDrag();
                OnSelectionChanged();
                break;
            }
        }
    }
}

