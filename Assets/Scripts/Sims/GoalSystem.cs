using System;
using System.Collections.Generic;

namespace OpenLife
{
    [Serializable]
    public class GoalProgress
    {
        public string id;
        public int count;
        public bool done;
    }

    /// <summary>Counts goal events, pays rewards when goals complete.</summary>
    public static class GoalSystem
    {
        public static readonly List<GoalProgress> Progress = new List<GoalProgress>();

        public static void Init()
        {
            Progress.Clear();
            foreach (var g in ContentDatabase.AllGoals())
                Progress.Add(new GoalProgress { id = g.id });
        }

        public static void Restore(List<GoalProgress> saved)
        {
            Init();
            if (saved == null) return;
            foreach (var s in saved)
            {
                var p = Progress.Find(x => x.id == s.id);
                if (p != null) { p.count = s.count; p.done = s.done; }
            }
        }

        public static void Fire(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            foreach (var p in Progress)
            {
                if (p.done) continue;
                var g = ContentDatabase.GetGoal(p.id);
                if (g == null || g.eventId != eventId) continue;
                p.count++;
                if (p.count >= g.target)
                {
                    p.done = true;
                    GameState.Funds += g.reward;
                    GameState.Notify($"Goal complete: {g.title} (+${g.reward})");
                }
            }
        }

        /// <summary>The next incomplete goals, in content order.</summary>
        public static List<GoalProgress> Active(int max)
        {
            var list = new List<GoalProgress>();
            foreach (var p in Progress)
            {
                if (p.done) continue;
                list.Add(p);
                if (list.Count >= max) break;
            }
            return list;
        }
    }
}
