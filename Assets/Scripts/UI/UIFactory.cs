using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace OpenLife
{
    /// <summary>Small helpers for building uGUI in code, so no scene or prefab setup is needed.</summary>
    public static class UIFactory
    {
        public static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.93f);
        public static readonly Color DarkText = new Color(0.14f, 0.16f, 0.2f);
        public static readonly Color Accent = new Color(0.28f, 0.72f, 0.36f);
        public static readonly Color Blue = new Color(0.26f, 0.55f, 0.92f);
        public static readonly Color Soft = new Color(0.9f, 0.92f, 0.95f);
        public static readonly Color Danger = new Color(0.9f, 0.32f, 0.3f);

        static Font font;

        public static Font Font
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        // ---------- canvas and events ----------

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = sortOrder;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            var module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        public static bool PointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public static bool IsTyping()
        {
            if (EventSystem.current == null) return false;
            var sel = EventSystem.current.currentSelectedGameObject;
            if (sel == null) return false;
            var f = sel.GetComponent<InputField>();
            return f != null && f.isFocused;
        }

        // ---------- layout ----------

        /// <summary>A rect positioned by anchor, pivot, position and size (all in canvas units).</summary>
        public static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color)
        {
            var rt = Box(parent, name, anchor, pivot, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing, int padding)
        {
            var g = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = true;
            g.childForceExpandHeight = false;
            return g;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing, int padding)
        {
            var g = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = true;
            return g;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, Vector2 spacing, int padding)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.padding = new RectOffset(padding, padding, padding, padding);
            return g;
        }

        /// <summary>Sets preferred size inside a layout group. Pass -1 to leave a dimension alone.</summary>
        public static void Size(Component c, float w, float h)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (w >= 0f) le.preferredWidth = w;
            if (h >= 0f) le.preferredHeight = h;
        }

        // ---------- widgets ----------

        public static Text Label(Transform parent, string text, int size, TextAnchor align, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, 0f);
            var t = go.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A label with its own rect, for use inside layout groups.</summary>
        public static Text Line(Transform parent, string text, int size, TextAnchor align, Color color, float height)
        {
            var rt = Box(parent, "Line", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, height));
            var t = Label(rt, text, size, align, color);
            Size(rt, -1f, height);
            return t;
        }

        public static Button MakeButton(Transform parent, string text, Action onClick, Color bg, int fontSize = 22)
        {
            var go = new GameObject("Button", typeof(RectTransform));
            go.GetComponent<RectTransform>().SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bg;
            var b = go.AddComponent<Button>();
            b.targetGraphic = img;
            var colors = b.colors;
            colors.normal = Color.white;
            colors.highlightedColor = new Color(0.92f, 0.96f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.78f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            Color txt = (bg.r + bg.g + bg.b) / 3f > 0.65f ? DarkText : Color.white;
            Label(go.transform, text, fontSize, TextAnchor.MiddleCenter, txt);
            return b;
        }

        public static void SetButtonText(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static void SetButtonColor(Button b, Color c)
        {
            var img = b.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        /// <summary>A horizontal bar. Use SetBar to change its value (0 to 1).</summary>
        public static Image MakeBar(Transform parent, Color fillColor)
        {
            var bg = new GameObject("Bar", typeof(RectTransform));
            bg.GetComponent<RectTransform>().SetParent(parent, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.82f, 0.85f, 0.88f);
            var fill = new GameObject("Fill", typeof(RectTransform));
            var rt = fill.GetComponent<RectTransform>();
            rt.SetParent(bg.transform, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = fill.AddComponent<Image>();
            img.color = fillColor;
            return img;
        }

        public static void SetBar(Image fill, float value01, Color color)
        {
            var rt = fill.rectTransform;
            rt.anchorMax = new Vector2(Mathf.Clamp01(value01), 1f);
            fill.color = color;
        }

        public static InputField MakeInput(Transform parent, string initial, string placeholder)
        {
            var go = new GameObject("Input", typeof(RectTransform));
            go.GetComponent<RectTransform>().SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = Soft;
            var field = go.AddComponent<InputField>();

            var text = Label(go.transform, "", 22, TextAnchor.MiddleLeft, DarkText);
            text.supportRichText = false;
            var ph = Label(go.transform, placeholder, 22, TextAnchor.MiddleLeft, new Color(0.5f, 0.52f, 0.56f));
            ph.fontStyle = FontStyle.Italic;

            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = img;
            field.text = initial;
            return field;
        }

        public static Image Swatch(Transform parent, Color color, Action onClick, bool outline)
        {
            var b = MakeButton(parent, "", onClick, color);
            var img = b.GetComponent<Image>();
            if (outline)
            {
                var o = b.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
                o.effectDistance = new Vector2(2f, -2f);
            }
            return img;
        }

        public static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }
    }
}
