using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenLife
{
    [Serializable]
    public class SaveFile
    {
        public int version = 1;
        public WorldSettings world;
        public int funds;
        public float totalMinutes;
        public int selectedIndex;
        public List<SimData> sims = new List<SimData>();
        public List<PlacedSave> placed = new List<PlacedSave>();
        public List<GoalProgress> goals = new List<GoalProgress>();
    }

    public static class SaveSystem
    {
        public static string PathFor(string slot)
        {
            return Path.Combine(Application.persistentDataPath, slot + ".json");
        }

        public static bool Exists(string slot = "save1")
        {
            return File.Exists(PathFor(slot));
        }

        public static bool Save(BuildMode build, string slot = "save1")
        {
            try
            {
                var file = new SaveFile
                {
                    world = GameState.World,
                    funds = GameState.Funds,
                    totalMinutes = GameClock.I != null ? GameClock.I.TotalMinutes : 0f,
                    placed = build.Export(),
                    goals = new List<GoalProgress>(GoalSystem.Progress)
                };
                foreach (var sim in GameState.Sims)
                {
                    if (sim == null) continue;
                    sim.StorePosition();
                    file.sims.Add(sim.Data);
                }
                file.selectedIndex = GameState.Sims.IndexOf(GameState.Selected);
                File.WriteAllText(PathFor(slot), JsonUtility.ToJson(file, true));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[OpenLife] Save failed: " + e.Message);
                return false;
            }
        }

        public static SaveFile Load(string slot = "save1")
        {
            try
            {
                string path = PathFor(slot);
                if (!File.Exists(path)) return null;
                return JsonUtility.FromJson<SaveFile>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogError("[OpenLife] Load failed: " + e.Message);
                return null;
            }
        }
    }
}
