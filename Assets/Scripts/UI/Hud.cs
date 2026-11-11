using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OpenLife
{
    /// <summary>
    /// The in-game HUD: goals panel, needs bars, mood, funds, household bar,
    /// speed controls and toast messages.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        class GoalRow { public Text left, right; }

        public Canvas Canvas { get; private set; }

        GoalRow[] goalRows;
        Image[] needFills;
        Text nameText, moodText, statusText, fundsText, timeText;
        Button[] speedButtons;
        Button autonomyButton, buildButton;
        RectTransform householdRow;
        readonly List<Button> simButtons = new List<Button>();
        readonly List<SimCharacter> simButtonRefs = new List<SimCharacter>();
        Panel toastPanel;
        readonly Queue<string> toasts = new Queue<string>();
        float toastTimer;

        class Panel { public RectTransform rt; public Text text; }

        public static Hud Create(Action onBuild, Action onSave, Action onMenu)
        {
            var go = new GameObject("Hud");
            var hud = go.AddComponent<Hud>();
            hud.Build(onBuild, onSave, onMenu);
            return hud;
        }

        void OnEnable() { GameState.ToastRaised += OnToast; }
        void OnDisable() { GameState.ToastRaised -= OnToast; }

        void OnToast(string msg) { toasts.Enqueue(msg); }

        // ---------- construction ----------

        void Build(Action onBuild, Action onSave, Action onMenu)
        {
            Canvas = UIFactory.CreateCanvas("HudCanvas", 10);
            Canvas.transform.SetParent(transform, false);
            var root = Canvas.transform;

            BuildGoals(root);
            BuildTopButtons(root, onBuild, onSave, onMenu);
            BuildSelected(root);
            BuildNeeds(root);
            BuildSpeed(root);
            BuildToast(root);

            var hint = UIFactory.Box(root, "Hint", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(1100f, 30f));
            var ht = UIFactory.Label(hint, "WASD pan  |  Q/E or right-drag rotate  |  Wheel zoom  |  F focus  |  B build  |  Space pause  |  F5 save", 18, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.85f));
            var shadow = ht.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        }

        void BuildGoals(Transform root)
        {
            var p = UIFactory.Panel(root, "Goals", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(400f, 220f), UIFactory.PanelColor);
            UIFactory.VStack(p, 6f, 12);
            var title = UIFactory.Line(p, "Main Goal", 28, TextAnchor.MiddleLeft, UIFactory.DarkText, 40f);
            title.fontStyle = FontStyle.Bold;

            goalRows = new GoalRow[3];
            for (int i = 0; i < 3; i++)
            {
                var row = UIFactory.Panel(p, "GoalRow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 44f), new Color(0.93f, 0.95f, 0.97f));
                UIFactory.Size(row, -1f, 44f);
                var l = UIFactory.Label(row, "", 24, TextAnchor.MiddleLeft, UIFactory.DarkText);
                var r = UIFactory.Label(row, "", 24, TextAnchor.MiddleRight, new Color(0.9f, 0.55f, 0.1f));
                goalRows[i] = new GoalRow { left = l, right = r };
            }
        }

        void BuildTopButtons(Transform root, Action onBuild, Action onSave, Action onMenu)
        {
            var row = UIFactory.Box(root, "TopButtons", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(520f, 60f));
            UIFactory.HStack(row, 10f, 0);
            buildButton = UIFactory.MakeButton(row, "Build (B)", onBuild, UIFactory.Blue);
            UIFactory.Size(buildButton, 170f, 60f);
            var save = UIFactory.MakeButton(row, "Save", onSave, UIFactory.Accent);
            UIFactory.Size(save, 150f, 60f);
            var menu = UIFactory.MakeButton(row, "Menu", onMenu, new Color(0.45f, 0.48f, 0.55f));
            UIFactory.Size(menu, 150f, 60f);
        }

        void BuildSelected(Transform root)
        {
            var p = UIFactory.Panel(root, "Selected", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 100f), new Vector2(520f, 210f), UIFactory.PanelColor);
            UIFactory.VStack(p, 4f, 12);
            nameText = UIFactory.Line(p, "", 30, TextAnchor.MiddleLeft, UIFactory.DarkText, 38f);
            nameText.fontStyle = FontStyle.Bold;
            moodText = UIFactory.Line(p, "", 24, TextAnchor.MiddleLeft, UIFactory.DarkText, 32f);
            statusText = UIFactory.Line(p, "", 22, TextAnchor.MiddleLeft, new Color(0.35f, 0.38f, 0.45f), 30f);

            var btns = UIFactory.Box(p, "Buttons", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 52f));
            UIFactory.Size(btns, -1f, 52f);
            UIFactory.HStack(btns, 8f, 0);
            var cancel = UIFactory.MakeButton(btns, "Cancel", () => { if (GameState.Selected != null) GameState.Selected.ClearAll(); }, UIFactory.Danger, 20);
            UIFactory.Size(cancel, 130f, 52f);
            autonomyButton = UIFactory.MakeButton(btns, "Free will: On", () =>
            {
                if (GameState.Selected != null) GameState.Selected.Autonomy = !GameState.Selected.Autonomy;
            }, UIFactory.Blue, 20);
            UIFactory.Size(autonomyButton, 190f, 52f);
            var focus = UIFactory.MakeButton(btns, "Focus (F)", () =>
            {
                var rig = FindFirstObjectByType<CameraRig>();
                if (rig != null && GameState.Selected != null) rig.FocusOn(GameState.Selected.transform.position);
            }, new Color(0.45f, 0.48f, 0.55f), 20);
            UIFactory.Size(focus, 150f, 52f);

            // household bar along the bottom-left
            var bar = UIFactory.Panel(root, "HouseholdBar", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(760f, 64f), UIFactory.PanelColor);
            UIFactory.HStack(bar, 8f, 6);
            var funds = UIFactory.Box(bar, "Funds", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 52f));
            UIFactory.Size(funds, 200f, 52f);
            fundsText = UIFactory.Label(funds, "", 28, TextAnchor.MiddleLeft, UIFactory.DarkText);
            fundsText.fontStyle = FontStyle.Bold;
            householdRow = UIFactory.Box(bar, "Sims", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 52f));
            UIFactory.Size(householdRow, 520f, 52f);
            UIFactory.HStack(householdRow, 6f, 0);
        }

        void BuildNeeds(Transform root)
        {
            var p = UIFactory.Panel(root, "Needs", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(560f, 260f), UIFactory.PanelColor);
            UIFactory.Grid(p, new Vector2(255f, 72f), new Vector2(10f, 6f), 12);
            string[] names = { "Hunger", "Energy", "Fun", "Social", "Hygiene", "Bladder" };
            // Display order like the concept art: left column then right column.
            int[] order = { 5, 2, 0, 3, 1, 4 };
            needFills = new Image[6];
            foreach (int idx in order)
            {
                var cell = UIFactory.Box(p, names[idx], new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(255f, 72f));
                var lbl = UIFactory.Box(cell, "t", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -2f), new Vector2(250f, 32f));
                UIFactory.Label(lbl, names[idx], 24, TextAnchor.MiddleLeft, UIFactory.DarkText);
                var fill = UIFactory.MakeBar(cell, UIFactory.Accent);
                var bg = fill.transform.parent.GetComponent<RectTransform>();
                bg.anchorMin = new Vector2(0f, 0f);
                bg.anchorMax = new Vector2(1f, 0f);
                bg.pivot = new Vector2(0.5f, 0f);
                bg.anchoredPosition = new Vector2(0f, 8f);
                bg.sizeDelta = new Vector2(-8f, 18f);
                needFills[idx] = fill;
            }
        }

        void BuildSpeed(Transform root)
        {
            var p = UIFactory.Panel(root, "Speed", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(105f, 24f), new Vector2(520f, 66f), UIFactory.PanelColor);
            UIFactory.HStack(p, 8f, 6);
            var t = UIFactory.Box(p, "Time", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220f, 54f));
            UIFactory.Size(t, 220f, 54f);
            timeText = UIFactory.Label(t, "", 22, TextAnchor.MiddleLeft, UIFactory.DarkText);
            timeText.fontStyle = FontStyle.Bold;

            speedButtons = new Button[4];
            string[] labels = { "II", "1x", "2x", "3x" };
            for (int i = 0; i < 4; i++)
            {
                int speed = i;
                speedButtons[i] = UIFactory.MakeButton(p, labels[i], () => { if (GameClock.I != null) GameClock.I.SetSpeed(speed); }, UIFactory.Soft, 24);
                UIFactory.Size(speedButtons[i], 60f, 54f);
            }
        }

        void BuildToast(Transform root)
        {
            var rt = UIFactory.Panel(root, "Toast", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(760f, 56f), new Color(0.12f, 0.14f, 0.18f, 0.92f));
            var txt = UIFactory.Label(rt, "", 24, TextAnchor.MiddleCenter, Color.white);
            toastPanel = new Panel { rt = rt, text = txt };
            rt.gameObject.SetActive(false);
        }

        // ---------- per-frame refresh ----------

        void Update()
        {
            var sim = GameState.Selected;
            if (sim == null && GameState.Sims.Count > 0) { sim = GameState.Sims[0]; GameState.Select(sim); }

            RefreshGoals();
            RefreshSelected(sim);
            RefreshNeeds(sim);
            RefreshHousehold();
            RefreshTime();
            RefreshToast();
            fundsText.text = "$ " + GameState.Funds.ToString("N0");
        }

        void RefreshGoals()
        {
            var active = GoalSystem.Active(3);
            for (int i = 0; i < 3; i++)
            {
                var row = goalRows[i];
                if (i < active.Count)
                {
                    var g = ContentDatabase.GetGoal(active[i].id);
                    row.left.text = g != null ? g.title : active[i].id;
                    row.right.text = active[i].count + " / " + (g != null ? g.target : 1);
                }
                else
                {
                    row.left.text = i == 0 ? "All goals complete!" : "";
                    row.right.text = "";
                }
            }
        }

        void RefreshSelected(SimCharacter sim)
        {
            if (sim == null)
            {
                nameText.text = "No Sim selected";
                moodText.text = "";
                statusText.text = "";
                return;
            }
            nameText.text = sim.Data.name;
            string mood = sim.Mood;
            moodText.text = mood + (sim.AverageNeeds < 70f ? "  (low " + sim.Weakest + ")" : "");
            moodText.color = MoodColor(mood);
            statusText.text = sim.StatusText + (sim.QueueLength > 1 ? "  +" + (sim.QueueLength - 1) + " queued" : "");
            UIFactory.SetButtonText(autonomyButton, "Free will: " + (sim.Autonomy ? "On" : "Off"));
        }

        void RefreshNeeds(SimCharacter sim)
        {
            for (int i = 0; i < 6; i++)
            {
                float v = sim != null ? sim.Data.needs[i] : 0f;
                UIFactory.SetBar(needFills[i], v / 100f, NeedColor(v));
            }
        }

        void RefreshHousehold()
        {
            bool changed = simButtonRefs.Count != GameState.Sims.Count;
            if (!changed)
                for (int i = 0; i < simButtonRefs.Count; i++)
                    if (simButtonRefs[i] != GameState.Sims[i]) { changed = true; break; }

            if (changed)
            {
                UIFactory.ClearChildren(householdRow);
                simButtons.Clear();
                simButtonRefs.Clear();
                foreach (var s in GameState.Sims)
                {
                    var sim = s;
                    Color skin = Util.ParseColor(sim.Data.skinHex, Color.gray);
                    var b = UIFactory.MakeButton(householdRow, ShortName(sim.Data.name), () => GameState.Select(sim), skin, 18);
                    UIFactory.Size(b, 120f, 52f);
                    var o = b.gameObject.AddComponent<Outline>();
                    o.effectColor = new Color(1f, 0.85f, 0.2f);
                    o.effectDistance = new Vector2(4f, -4f);
                    o.enabled = false;
                    simButtons.Add(b);
                    simButtonRefs.Add(sim);
                }
            }

            for (int i = 0; i < simButtons.Count; i++)
            {
                var o = simButtons[i].GetComponent<Outline>();
                if (o != null) o.enabled = simButtonRefs[i] == GameState.Selected;
            }
        }

        void RefreshTime()
        {
            var clock = GameClock.I;
            if (clock == null) return;
            timeText.text = "Day " + (clock.Day + 1) + "  " + clock.Label;
            for (int i = 0; i < 4; i++)
                UIFactory.SetButtonColor(speedButtons[i], i == clock.Speed ? UIFactory.Accent : UIFactory.Soft);
        }

        void RefreshToast()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toastPanel.rt.gameObject.SetActive(false);
            }
            if (toastTimer <= 0f && toasts.Count > 0)
            {
                toastPanel.text.text = toasts.Dequeue();
                toastPanel.rt.gameObject.SetActive(true);
                toastTimer = 3f;
            }
        }

        public void SetVisible(bool v) { Canvas.gameObject.SetActive(v); }

        // ---------- helpers ----------

        static string ShortName(string n)
        {
            if (string.IsNullOrEmpty(n)) return "?";
            return n.Length > 9 ? n.Substring(0, 9) : n;
        }

        static Color NeedColor(float v)
        {
            if (v > 50f) return UIFactory.Accent;
            if (v > 25f) return new Color(1f, 0.75f, 0.2f);
            return UIFactory.Danger;
        }

        static Color MoodColor(string mood)
        {
            switch (mood)
            {
                case "Happy": return new Color(0.2f, 0.62f, 0.3f);
                case "Fine": return new Color(0.2f, 0.45f, 0.85f);
                case "Uncomfortable": return new Color(0.85f, 0.55f, 0.05f);
                default: return UIFactory.Danger;
            }
        }
    }
}
