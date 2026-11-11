using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenLife
{
    /// <summary>Start screen: world creator on the left, Sim creator on the right.</summary>
    public class StartScreen : MonoBehaviour
    {
        static readonly string[] SimNames =
        {
            "Amara", "Zuri", "Kato", "Nia", "Tendo", "Imani", "Jabari", "Sasha", "Leo", "Mia",
            "Kofi", "Aisha", "Mateo", "Lina", "Omar", "Yuki", "Ravi", "Elena"
        };

        public event Action<WorldSettings, List<SimData>> StartRequested;
        public event Action LoadRequested;

        Canvas canvas;
        RectTransform worldPanel;
        RectTransform simPanel;
        RectTransform bottom;

        WorldSettings settings = new WorldSettings();
        SimData draft;
        readonly List<SimData> household = new List<SimData>();
        readonly HashSet<string> chosenTraits = new HashSet<string>();
        List<WorldDef> worlds = new List<WorldDef>();
        int worldIndex;

        public static StartScreen Create()
        {
            var go = new GameObject("StartScreen");
            var s = go.AddComponent<StartScreen>();
            s.Init();
            return s;
        }

        void Init()
        {
            canvas = UIFactory.CreateCanvas("StartCanvas", 30);
            canvas.transform.SetParent(transform, false);

            var bg = UIFactory.Stretch(canvas.transform, "Background");
            bg.gameObject.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.13f, 0.97f);

            var titleBox = UIFactory.Box(canvas.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(900f, 80f));
            var title = UIFactory.Label(titleBox, "OpenLife", 64, TextAnchor.MiddleCenter, Color.white);
            title.fontStyle = FontStyle.Bold;
            var subBox = UIFactory.Box(canvas.transform, "Sub", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(900f, 34f));
            UIFactory.Label(subBox, "Create your world. Create your people. Live your story.", 24, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.7f));

            worldPanel = UIFactory.Panel(canvas.transform, "WorldPanel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(50f, -10f), new Vector2(860f, 780f), UIFactory.PanelColor);
            simPanel = UIFactory.Panel(canvas.transform, "SimPanel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-50f, -10f), new Vector2(860f, 780f), UIFactory.PanelColor);
            bottom = UIFactory.Box(canvas.transform, "Bottom", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(1100f, 80f));
            var modsBox = UIFactory.Box(canvas.transform, "ModsHint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(1500f, 24f));
            UIFactory.Label(modsBox, "Mods folder: " + ContentDatabase.ModsPath, 16, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.5f));

            worlds = ContentDatabase.AllWorlds();
            if (worlds.Count > 0)
            {
                settings.worldId = worlds[0].id;
                settings.startHour = worlds[0].startHour;
            }
            settings.seed = UnityEngine.Random.Range(1, 99999);
            NewDraft(true);
            Rebuild();
        }

        public void Show() { canvas.gameObject.SetActive(true); Rebuild(); }
        public void Hide() { canvas.gameObject.SetActive(false); }

        // ---------- state ----------

        void NewDraft(bool resetLook)
        {
            var old = draft;
            draft = new SimData { name = SimNames[UnityEngine.Random.Range(0, SimNames.Length)] };
            if (resetLook || old == null)
            {
                draft.skinHex = Palettes.SkinTones[4];
                draft.hairHex = Palettes.HairColors[0];
                draft.outfitHex = Palettes.OutfitColors[0];
                draft.hairStyle = Palettes.HairStyles[0];
            }
            else
            {
                draft.skinHex = old.skinHex;
                draft.hairHex = old.hairHex;
                draft.outfitHex = old.outfitHex;
                draft.hairStyle = old.hairStyle;
            }
            chosenTraits.Clear();
        }

        void AddDraft()
        {
            if (household.Count >= 4) { GameState.Notify("A household holds up to 4 Sims."); return; }
            if (string.IsNullOrWhiteSpace(draft.name)) draft.name = SimNames[UnityEngine.Random.Range(0, SimNames.Length)];
            draft.name = MakeUniqueName(draft.name.Trim());
            draft.traitIds = new List<string>(chosenTraits).ToArray();
            household.Add(draft);
            NewDraft(false);
            Rebuild();
        }

        string MakeUniqueName(string n)
        {
            string name = n;
            int i = 2;
            while (household.Exists(s => s.name == name)) name = n + " " + (i++);
            return name;
        }

        // ---------- UI build ----------

        void Rebuild()
        {
            UIFactory.ClearChildren(worldPanel);
            UIFactory.ClearChildren(simPanel);
            UIFactory.ClearChildren(bottom);
            if (worldPanel.GetComponent<VerticalLayoutGroup>() == null) UIFactory.VStack(worldPanel, 6f, 12);
            if (simPanel.GetComponent<VerticalLayoutGroup>() == null) UIFactory.VStack(simPanel, 6f, 12);
            BuildWorldPanel();
            BuildSimPanel();
            BuildBottom();
        }

        RectTransform Row(Transform parent, float h, float spacing = 8f)
        {
            var r = UIFactory.Box(parent, "Row", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, h));
            UIFactory.Size(r, -1f, h);
            UIFactory.HStack(r, spacing, 0);
            return r;
        }

        void RowLabel(Transform row, string text, float width)
        {
            var b = UIFactory.Box(row, "L", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 40f));
            UIFactory.Size(b, width, -1f);
            UIFactory.Label(b, text, 22, TextAnchor.MiddleLeft, UIFactory.DarkText);
        }

        Button RowButton(Transform row, string text, float width, Action act, Color color, int size = 22)
        {
            var b = UIFactory.MakeButton(row, text, act, color, size);
            UIFactory.Size(b, width, -1f);
            return b;
        }

        void Heading(Transform parent, string text)
        {
            var t = UIFactory.Line(parent, text, 30, TextAnchor.MiddleLeft, UIFactory.DarkText, 44f);
            t.fontStyle = FontStyle.Bold;
        }

        void SwatchRow(Transform parent, string label, string[] hexes, string current, Action<string> pick, bool allowDefault)
        {
            UIFactory.Line(parent, label, 20, TextAnchor.MiddleLeft, new Color(0.35f, 0.38f, 0.45f), 26f);
            var row = Row(parent, 46f, 6f);
            if (allowDefault)
            {
                bool sel = string.IsNullOrEmpty(current);
                var b = RowButton(row, "Preset", 90f, () => { pick(""); Rebuild(); }, UIFactory.Soft, 18);
                Mark(b, sel);
            }
            foreach (var hex in hexes)
            {
                string h = hex;
                var b = RowButton(row, "", 46f, () => { pick(h); Rebuild(); }, Util.ParseColor(h, Color.gray));
                Mark(b, string.Equals(current, h, StringComparison.OrdinalIgnoreCase));
            }
        }

        static void Mark(Button b, bool selected)
        {
            var o = b.gameObject.AddComponent<Outline>();
            o.effectColor = selected ? new Color(1f, 0.8f, 0.1f) : new Color(0.1f, 0.1f, 0.1f, 0.45f);
            o.effectDistance = selected ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
        }

        void BuildWorldPanel()
        {
            Heading(worldPanel, "1. Create your world");

            WorldDef def = worlds.Count > 0 ? worlds[Mathf.Clamp(worldIndex, 0, worlds.Count - 1)] : null;

            var presetRow = Row(worldPanel, 56f);
            RowButton(presetRow, "<", 70f, () => CyclePreset(-1), UIFactory.Soft, 26);
            var nameBox = UIFactory.Box(presetRow, "Preset", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 56f));
            UIFactory.Size(nameBox, 560f, -1f);
            var presetName = UIFactory.Label(nameBox, def != null ? def.name : "No worlds found", 30, TextAnchor.MiddleCenter, UIFactory.DarkText);
            presetName.fontStyle = FontStyle.Bold;
            RowButton(presetRow, ">", 70f, () => CyclePreset(1), UIFactory.Soft, 26);

            UIFactory.Line(worldPanel, def != null ? def.description : "", 20, TextAnchor.MiddleCenter, new Color(0.35f, 0.38f, 0.45f), 30f);

            var nameRow = Row(worldPanel, 52f);
            RowLabel(nameRow, "World name", 190f);
            var input = UIFactory.MakeInput(nameRow, settings.displayName, "Name your world");
            UIFactory.Size(input, 600f, -1f);
            input.onValueChanged.AddListener(v => settings.displayName = v);

            SwatchRow(worldPanel, "Ground color", Palettes.GroundColors, settings.groundHex, h => settings.groundHex = h, true);
            SwatchRow(worldPanel, "Sky color", Palettes.SkyColors, settings.skyHex, h => settings.skyHex = h, true);

            var densRow = Row(worldPanel, 52f);
            RowLabel(densRow, "Nature / props", 260f);
            RowButton(densRow, "-", 60f, () => { settings.propScale = Mathf.Max(0f, settings.propScale - 0.25f); Rebuild(); }, UIFactory.Soft, 28);
            var dBox = UIFactory.Box(densRow, "D", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 40f));
            UIFactory.Size(dBox, 140f, -1f);
            UIFactory.Label(dBox, Mathf.RoundToInt(settings.propScale * 100f) + "%", 24, TextAnchor.MiddleCenter, UIFactory.DarkText);
            RowButton(densRow, "+", 60f, () => { settings.propScale = Mathf.Min(3f, settings.propScale + 0.25f); Rebuild(); }, UIFactory.Soft, 28);

            var hourRow = Row(worldPanel, 52f);
            RowLabel(hourRow, "Start time", 260f);
            RowButton(hourRow, "-", 60f, () => { settings.startHour = (settings.startHour + 23) % 24; Rebuild(); }, UIFactory.Soft, 28);
            var hBox = UIFactory.Box(hourRow, "H", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 40f));
            UIFactory.Size(hBox, 140f, -1f);
            UIFactory.Label(hBox, settings.startHour.ToString("00") + ":00", 24, TextAnchor.MiddleCenter, UIFactory.DarkText);
            RowButton(hourRow, "+", 60f, () => { settings.startHour = (settings.startHour + 1) % 24; Rebuild(); }, UIFactory.Soft, 28);

            var seedRow = Row(worldPanel, 52f);
            RowLabel(seedRow, "Layout seed", 260f);
            var sBox = UIFactory.Box(seedRow, "S", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140f, 40f));
            UIFactory.Size(sBox, 140f, -1f);
            UIFactory.Label(sBox, settings.seed.ToString(), 24, TextAnchor.MiddleCenter, UIFactory.DarkText);
            RowButton(seedRow, "Randomize", 200f, () => { settings.seed = UnityEngine.Random.Range(1, 99999); Rebuild(); }, UIFactory.Blue, 20);

            var lotRow = Row(worldPanel, 52f);
            RowLabel(lotRow, "Start with", 260f);
            RowButton(lotRow, settings.emptyLot ? "Empty lot" : "Furnished starter home", 360f, () => { settings.emptyLot = !settings.emptyLot; Rebuild(); }, UIFactory.Blue, 20);
        }

        void CyclePreset(int dir)
        {
            if (worlds.Count == 0) return;
            worldIndex = (worldIndex + dir + worlds.Count) % worlds.Count;
            var d = worlds[worldIndex];
            settings.worldId = d.id;
            settings.groundHex = "";
            settings.skyHex = "";
            settings.startHour = d.startHour;
            Rebuild();
        }

        void BuildSimPanel()
        {
            Heading(simPanel, "2. Create your household (" + household.Count + "/4)");

            var nameRow = Row(simPanel, 52f);
            RowLabel(nameRow, "Name", 120f);
            var input = UIFactory.MakeInput(nameRow, draft.name, "Sim name");
            UIFactory.Size(input, 420f, -1f);
            input.onValueChanged.AddListener(v => draft.name = v);
            RowButton(nameRow, "Random", 150f, () => { draft.name = SimNames[UnityEngine.Random.Range(0, SimNames.Length)]; Rebuild(); }, UIFactory.Soft, 20);

            SwatchRow(simPanel, "Skin tone", Palettes.SkinTones, draft.skinHex, h => draft.skinHex = h, false);
            SwatchRow(simPanel, "Hair color", Palettes.HairColors, draft.hairHex, h => draft.hairHex = h, false);
            SwatchRow(simPanel, "Outfit", Palettes.OutfitColors, draft.outfitHex, h => draft.outfitHex = h, false);

            var styleRow = Row(simPanel, 50f);
            RowLabel(styleRow, "Hair style", 160f);
            RowButton(styleRow, "<", 60f, () => CycleStyle(-1), UIFactory.Soft, 26);
            var stBox = UIFactory.Box(styleRow, "St", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 40f));
            UIFactory.Size(stBox, 220f, -1f);
            UIFactory.Label(stBox, draft.hairStyle, 24, TextAnchor.MiddleCenter, UIFactory.DarkText);
            RowButton(styleRow, ">", 60f, () => CycleStyle(1), UIFactory.Soft, 26);

            UIFactory.Line(simPanel, "Traits (pick up to 3): " + chosenTraits.Count + "/3", 20, TextAnchor.MiddleLeft, new Color(0.35f, 0.38f, 0.45f), 26f);
            var grid = UIFactory.Box(simPanel, "Traits", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 170f));
            UIFactory.Size(grid, -1f, 170f);
            UIFactory.Grid(grid, new Vector2(262f, 38f), new Vector2(6f, 4f), 0);
            foreach (var t in ContentDatabase.AllTraits())
            {
                var trait = t;
                bool on = chosenTraits.Contains(trait.id);
                UIFactory.MakeButton(grid, trait.name, () => ToggleTrait(trait.id), on ? UIFactory.Accent : UIFactory.Soft, 20);
            }

            var addRow = Row(simPanel, 56f);
            RowButton(addRow, "Add to household", 300f, AddDraft, UIFactory.Blue, 24);
            var hint = UIFactory.Box(addRow, "Hint", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 40f));
            UIFactory.Size(hint, 500f, -1f);
            UIFactory.Label(hint, "Want more traits? Add them in traits.json (see MODDING.md).", 18, TextAnchor.MiddleLeft, new Color(0.45f, 0.48f, 0.55f));

            var listRow = Row(simPanel, 50f, 6f);
            foreach (var s in household)
            {
                var sim = s;
                var b = RowButton(listRow, sim.name + "  x", 190f, () => { household.Remove(sim); Rebuild(); }, Util.ParseColor(sim.skinHex, Color.gray), 18);
                Mark(b, false);
            }
        }

        void CycleStyle(int dir)
        {
            int i = Array.IndexOf(Palettes.HairStyles, draft.hairStyle);
            if (i < 0) i = 0;
            i = (i + dir + Palettes.HairStyles.Length) % Palettes.HairStyles.Length;
            draft.hairStyle = Palettes.HairStyles[i];
            Rebuild();
        }

        void ToggleTrait(string id)
        {
            if (chosenTraits.Contains(id)) chosenTraits.Remove(id);
            else if (chosenTraits.Count < 3) chosenTraits.Add(id);
            Rebuild();
        }

        void BuildBottom()
        {
            if (bottom.GetComponent<HorizontalLayoutGroup>() == null) UIFactory.HStack(bottom, 16f, 0);
            var start = UIFactory.MakeButton(bottom, "Start New Game", OnStart, UIFactory.Accent, 30);
            UIFactory.Size(start, 360f, 80f);
            if (SaveSystem.Exists())
            {
                var load = UIFactory.MakeButton(bottom, "Load Game", () => { if (LoadRequested != null) LoadRequested(); }, UIFactory.Blue, 30);
                UIFactory.Size(load, 260f, 80f);
            }
            var quit = UIFactory.MakeButton(bottom, "Quit", Application.Quit, new Color(0.45f, 0.48f, 0.55f), 30);
            UIFactory.Size(quit, 200f, 80f);
        }

        void OnStart()
        {
            if (household.Count == 0)
            {
                draft.traitIds = new List<string>(chosenTraits).ToArray();
                if (string.IsNullOrWhiteSpace(draft.name)) draft.name = SimNames[0];
                household.Add(draft);
            }
            if (string.IsNullOrWhiteSpace(settings.displayName)) settings.displayName = "My World";
            var copy = JsonUtility.FromJson<WorldSettings>(JsonUtility.ToJson(settings));
            var sims = new List<SimData>(household);
            household.Clear();
            NewDraft(false);
            if (StartRequested != null) StartRequested(copy, sims);
        }
    }
}
