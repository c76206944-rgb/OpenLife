using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenLife
{
    /// <summary>
    /// Boots the whole game in any scene: camera, UI, world, Sims.
    /// Open any scene (even an empty one) and press Play.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<GameController>() != null) return;
            new GameObject("OpenLife").AddComponent<GameController>();
        }

        Camera cam;
        CameraRig rig;
        WorldBuilder world;
        BuildMode build;
        GameClock clock;
        Hud hud;
        InteractionMenu menu;
        InteractionController interaction;
        StartScreen start;
        bool playing;

        void Awake()
        {
            ContentDatabase.Load();
            UIFactory.EnsureEventSystem();

            cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.InputEnabled = false;

            clock = gameObject.AddComponent<GameClock>();
            clock.SetSpeed(0);

            world = new GameObject("World").AddComponent<WorldBuilder>();
            build = gameObject.AddComponent<BuildMode>();
            build.Init(world, cam);

            menu = InteractionMenu.Create();
            interaction = gameObject.AddComponent<InteractionController>();
            interaction.Init(cam, build, menu);
            interaction.Enabled = false;

            hud = Hud.Create(() => { if (playing) build.Toggle(); }, SaveGame, ShowStart);
            hud.SetVisible(false);
            BuildPanel.Create(build);

            start = StartScreen.Create();
            start.StartRequested += (ws, sims) => StartGame(ws, sims, null);
            start.LoadRequested += LoadGame;
        }

        // ---------- game flow ----------

        void ClearGame()
        {
            build.SetActive(false);
            menu.Hide();
            foreach (var s in new List<SimCharacter>(GameState.Sims))
            {
                if (s == null) continue;
                s.gameObject.SetActive(false);
                Destroy(s.gameObject);
            }
            build.ClearAll();
            GameState.Reset();
        }

        void StartGame(WorldSettings ws, List<SimData> sims, SaveFile save)
        {
            ClearGame();
            GameState.World = ws;
            world.Build(ws, cam);
            GoalSystem.Init();

            clock.SetTime(0, ws.startHour);

            if (save != null)
            {
                GameState.Funds = save.funds;
                clock.TotalMinutes = save.totalMinutes;
                GoalSystem.Restore(save.goals);
                build.Import(save.placed);
            }
            else if (!ws.emptyLot)
            {
                build.BuildStarterLot();
            }

            for (int i = 0; i < sims.Count; i++)
            {
                var d = sims[i];
                Vector3 pos;
                if (save != null) pos = new Vector3(d.x, d.y, d.z);
                else pos = new Vector3(-1.5f + i, 0f, -0.5f);
                SimFactory.Create(d, pos);
            }

            if (GameState.Sims.Count > 0)
            {
                int sel = save != null ? Mathf.Clamp(save.selectedIndex, 0, GameState.Sims.Count - 1) : 0;
                GameState.Select(GameState.Sims[sel]);
                rig.FocusOn(GameState.Sims[sel].transform.position);
            }

            clock.SetSpeed(1);
            start.Hide();
            hud.SetVisible(true);
            rig.InputEnabled = true;
            interaction.Enabled = true;
            playing = true;
        }

        void LoadGame()
        {
            var save = SaveSystem.Load();
            if (save == null || save.world == null || save.sims == null || save.sims.Count == 0)
            {
                GameState.Notify("Could not load the save.");
                return;
            }
            StartGame(save.world, save.sims, save);
            GameState.Notify("Game loaded.");
        }

        void SaveGame()
        {
            if (!playing) return;
            GameState.Notify(SaveSystem.Save(build) ? "Game saved." : "Save failed, see console.");
        }

        void ShowStart()
        {
            if (!playing) return;
            SaveSystem.Save(build);
            playing = false;
            build.SetActive(false);
            menu.Hide();
            clock.SetSpeed(0);
            hud.SetVisible(false);
            rig.InputEnabled = false;
            interaction.Enabled = false;
            start.Show();
        }

        // ---------- hotkeys ----------

        void Update()
        {
            if (!playing) return;
            var kb = Keyboard.current;
            if (kb == null || UIFactory.IsTyping()) return;

            if (kb.spaceKey.wasPressedThisFrame) clock.SetSpeed(clock.Speed == 0 ? 1 : 0);
            if (kb.digit1Key.wasPressedThisFrame) clock.SetSpeed(1);
            if (kb.digit2Key.wasPressedThisFrame) clock.SetSpeed(2);
            if (kb.digit3Key.wasPressedThisFrame) clock.SetSpeed(3);
            if (kb.bKey.wasPressedThisFrame) build.Toggle();
            if (kb.f5Key.wasPressedThisFrame) SaveGame();

            if (kb.tabKey.wasPressedThisFrame && GameState.Sims.Count > 1)
            {
                int i = GameState.Sims.IndexOf(GameState.Selected);
                GameState.Select(GameState.Sims[(i + 1) % GameState.Sims.Count]);
            }
        }

        void OnApplicationQuit()
        {
            if (playing) SaveSystem.Save(build);
        }
    }
}
