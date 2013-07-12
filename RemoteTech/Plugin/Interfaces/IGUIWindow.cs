﻿using System;
using UnityEngine;
using System.Reflection;
using Random = System.Random;

namespace RemoteTech {
    public enum WindowAlign {
        Floating,
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft,
    }

    internal interface IGUIWindow {
        void Show();
        void Hide();
    }

    public abstract class AbstractWindow : IGUIWindow {
        public Rect Position;
        public String Title { get; set; }
        public bool Enabled = false;
        public static GUIStyle Frame = new GUIStyle(HighLogic.Skin.window);
        
        private readonly WindowAlign mAlign;
        private readonly int mWindowId;
        private readonly bool[] mUsedPointers;

        static AbstractWindow() {
            Frame.padding = new RectOffset(5, 5, 5, 5);
        }

        public AbstractWindow(String title, Rect position, WindowAlign align) {
            Title = title;
            mAlign = align;
            Position = position;
            mWindowId = (new Random()).Next();

            // Dirty reflection hack to remove EZGUI mouse events over the Unity GUI.
            // Not illegal, no decompilation necessary to obtain data.
            mUsedPointers = (bool[]) typeof(UIManager).GetField("usedPointers",
    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(UIManager.instance);
        }

        public virtual void Show() {
            if (Enabled)
                return;
            Enabled = true;
            RTUtil.Log("Enabled");
            RenderingManager.AddToPostDrawQueue(0, Draw);
            UIManager.instance.AddMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public virtual void Hide() {
            if (!Enabled)
                return;
            Enabled = false;
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
            UIManager.instance.RemoveMouseTouchPtrListener(EZGUIMouseTouchPtrListener);
        }

        public virtual void Window(int uid) {
            if (Title != null) {
                if (GUI.Button(new Rect(Position.width - 18, 2, 16, 16), "")) {
                    Hide();
                }
                GUI.DragWindow(new Rect(0, 0, 100000, 20));
            }
            if (Event.current.isMouse && Position.ContainsMouse()) {
                Event.current.Use();
            }
        }

        protected virtual void Draw() {
            switch (mAlign) {
                default:
                    break;
                case WindowAlign.BottomLeft:
                    Position.x = 0;
                    Position.y = Screen.height - Position.height;
                    break;
                case WindowAlign.BottomRight:
                    Position.x = Screen.width - Position.width;
                    Position.y = Screen.height - Position.height;
                    break;
                case WindowAlign.TopLeft:
                    Position.x = 0;
                    Position.y = 0;
                    break;
                case WindowAlign.TopRight:
                    Position.x = Screen.width - Position.width;
                    Position.y = 0;
                    break;
            }
            if (Event.current.type == EventType.Layout) {
                Position.width = 0;
                Position.height = 0;
            }
            if (Title == null) {
                Position = GUILayout.Window(mWindowId, Position, Window, Title, GUIStyle.none);
            } else {
                Position = GUILayout.Window(mWindowId, Position, Window, Title);
            }
        }

        private void EZGUIMouseTouchPtrListener(POINTER_INFO ptr) {
            if (Position.Contains(new Vector2(ptr.devicePos.x, Screen.height - ptr.devicePos.y))) {
                mUsedPointers[0] = true;
            }
        }
    }
}
