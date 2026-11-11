using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenLife
{
    /// <summary>A small popup menu shown next to the mouse.</summary>
    public class InteractionMenu : MonoBehaviour
    {
        Canvas canvas;
        RectTransform panel;

        public bool Visible { get { return panel != null && panel.gameObject.activeSelf; } }

        public static InteractionMenu Create()
        {
            var go = new GameObject("InteractionMenu");
            var m = go.AddComponent<InteractionMenu>();
            m.canvas = UIFactory.CreateCanvas("MenuCanvas", 20);
            m.canvas.transform.SetParent(go.transform, false);
            return m;
        }

        public void Show(string title, List<KeyValuePair<string, Action>> items, Vector2 screenPos)
        {
            Hide();
            if (items == null || items.Count == 0) return;

            float scale = Mathf.Max(0.01f, canvas.scaleFactor);
            float w = 320f;
            float h = 24f + 46f + items.Count * 50f;
            Vector2 pos = screenPos / scale;
            float maxX = Screen.width / scale;
            if (pos.x + w > maxX) pos.x = maxX - w - 8f;
            if (pos.y - h < 0f) pos.y = h + 8f;

            panel = UIFactory.Panel(canvas.transform, "Menu", Vector2.zero, new Vector2(0f, 1f), pos, new Vector2(w, h), UIFactory.PanelColor);
            UIFactory.VStack(panel, 4f, 10);
            var t = UIFactory.Line(panel, title, 24, TextAnchor.MiddleLeft, UIFactory.DarkText, 42f);
            t.fontStyle = FontStyle.Bold;

            foreach (var item in items)
            {
                var act = item.Value;
                var b = UIFactory.MakeButton(panel, item.Key, () => { Hide(); if (act != null) act(); }, UIFactory.Soft, 22);
                UIFactory.Size(b, -1f, 46f);
            }
        }

        public void Hide()
        {
            if (panel != null) Destroy(panel.gameObject);
            panel = null;
        }

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (Visible && kb != null && kb.escapeKey.wasPressedThisFrame) Hide();
        }
    }
}
