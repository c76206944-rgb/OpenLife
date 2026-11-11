using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenLife
{
    /// <summary>Side panel shown in build mode: categories, items, colors and tools.</summary>
    public class BuildPanel : MonoBehaviour
    {
        const int ItemsPerPage = 7;

        BuildMode build;
        Canvas canvas;
        RectTransform categoriesRoot;
        RectTransform itemsRoot;
        RectTransform colorsRoot;
        Text infoText;
        Text pageText;
        int catIndex;
        int page;
        List<string> categories = new List<string>();

        public static BuildPanel Create(BuildMode b)
        {
            var go = new GameObject("BuildPanel");
            var bp = go.AddComponent<BuildPanel>();
            bp.Init(b);
            return bp;
        }

        void Init(BuildMode b)
        {
            build = b;
            canvas = UIFactory.CreateCanvas("BuildCanvas", 11);
            canvas.transform.SetParent(transform, false);

            var p = UIFactory.Panel(canvas.transform, "Panel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(340f, 800f), UIFactory.PanelColor);
            UIFactory.VStack(p, 6f, 12);

            var title = UIFactory.Line(p, "Build Mode", 30, TextAnchor.MiddleLeft, UIFactory.DarkText, 40f);
            title.fontStyle = FontStyle.Bold;

            categoriesRoot = UIFactory.Box(p, "Categories", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 160f));
            UIFactory.Size(categoriesRoot, -1f, 160f);
            UIFactory.Grid(categoriesRoot, new Vector2(96f, 38f), new Vector2(6f, 6f), 0);

            itemsRoot = UIFactory.Box(p, "Items", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 340f));
            UIFactory.Size(itemsRoot, -1f, 340f);
            UIFactory.VStack(itemsRoot, 4f, 0);

            var pager = UIFactory.Box(p, "Pager", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 44f));
            UIFactory.Size(pager, -1f, 44f);
            UIFactory.HStack(pager, 8f, 0);
            var prev = UIFactory.MakeButton(pager, "<", () => { page = Mathf.Max(0, page - 1); RefreshItems(); }, UIFactory.Soft, 24);
            UIFactory.Size(prev, 70f, 44f);
            var pt = UIFactory.Box(pager, "PageText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 44f));
            UIFactory.Size(pt, 140f, 44f);
            pageText = UIFactory.Label(pt, "", 22, TextAnchor.MiddleCenter, UIFactory.DarkText);
            var next = UIFactory.MakeButton(pager, ">", () => { page++; RefreshItems(); }, UIFactory.Soft, 24);
            UIFactory.Size(next, 70f, 44f);

            colorsRoot = UIFactory.Box(p, "Colors", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 44f));
            UIFactory.Size(colorsRoot, -1f, 44f);
            UIFactory.Grid(colorsRoot, new Vector2(34f, 34f), new Vector2(4f, 4f), 2);

            infoText = UIFactory.Line(p, "", 20, TextAnchor.MiddleLeft, new Color(0.35f, 0.38f, 0.45f), 52f);

            var tools = UIFactory.Box(p, "Tools", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300f, 52f));
            UIFactory.Size(tools, -1f, 52f);
            UIFactory.HStack(tools, 8f, 0);
            var del = UIFactory.MakeButton(tools, "Delete", () => build.SelectDelete(), UIFactory.Danger, 22);
            UIFactory.Size(del, 130f, 52f);
            var done = UIFactory.MakeButton(tools, "Done", () => build.SetActive(false), UIFactory.Accent, 22);
            UIFactory.Size(done, 130f, 52f);

            BuildColorSwatches();
            build.StateChanged += OnStateChanged;
            OnStateChanged();
        }

        void OnDestroy()
        {
            if (build != null) build.StateChanged -= OnStateChanged;
        }

        void OnStateChanged()
        {
            canvas.gameObject.SetActive(build.Active);
            if (!build.Active) return;
            RefreshCategories();
            RefreshItems();
            RefreshInfo();
        }

        void BuildColorSwatches()
        {
            UIFactory.ClearChildren(colorsRoot);
            foreach (var hex in Palettes.BuildColors)
            {
                string h = hex;
                Color c = string.IsNullOrEmpty(h) ? Color.white : Util.ParseColor(h, Color.white);
                var b = UIFactory.MakeButton(colorsRoot, string.IsNullOrEmpty(h) ? "x" : "", () => build.SetColor(h), c, 20);
                var o = b.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
                o.effectDistance = new Vector2(2f, -2f);
            }
        }

        void RefreshCategories()
        {
            categories = ContentDatabase.ObjectCategories();
            if (catIndex >= categories.Count) catIndex = 0;
            UIFactory.ClearChildren(categoriesRoot);
            for (int i = 0; i < categories.Count; i++)
            {
                int idx = i;
                var b = UIFactory.MakeButton(categoriesRoot, categories[i], () => { catIndex = idx; page = 0; RefreshCategories(); RefreshItems(); }, i == catIndex ? UIFactory.Blue : UIFactory.Soft, 18);
            }
        }

        void RefreshItems()
        {
            UIFactory.ClearChildren(itemsRoot);
            if (categories.Count == 0) { pageText.text = ""; return; }

            var items = new List<ObjectDef>();
            foreach (var o in ContentDatabase.AllObjects())
                if (o.category == categories[catIndex]) items.Add(o);

            int pages = Mathf.Max(1, Mathf.CeilToInt(items.Count / (float)ItemsPerPage));
            page = Mathf.Clamp(page, 0, pages - 1);
            pageText.text = (page + 1) + " / " + pages;

            for (int i = page * ItemsPerPage; i < Mathf.Min(items.Count, (page + 1) * ItemsPerPage); i++)
            {
                var d = items[i];
                bool sel = build.Selected == d;
                var b = UIFactory.MakeButton(itemsRoot, d.name + "   $" + d.price, () => build.Select(d), sel ? UIFactory.Accent : UIFactory.Soft, 20);
                UIFactory.Size(b, -1f, 44f);
            }
        }

        void RefreshInfo()
        {
            if (build.DeleteTool) infoText.text = "Click an object to delete it (50% refund).";
            else if (build.Selected != null) infoText.text = build.Selected.name + ": click to place, R to rotate, hold to paint. Esc to cancel.";
            else infoText.text = "Pick an item. Choose a color below, then place.";
        }
    }
}
