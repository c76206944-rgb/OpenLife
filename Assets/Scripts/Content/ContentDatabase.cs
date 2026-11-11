using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OpenLife
{
    /// <summary>
    /// Loads all JSON content packs. Built-in packs come from StreamingAssets/Content,
    /// mods from persistentDataPath/Mods. Items merge by id, later files override earlier ones.
    /// </summary>
    public static class ContentDatabase
    {
        static readonly Dictionary<string, TraitDef> traits = new Dictionary<string, TraitDef>();
        static readonly Dictionary<string, ObjectDef> objects = new Dictionary<string, ObjectDef>();
        static readonly Dictionary<string, GoalDef> goals = new Dictionary<string, GoalDef>();
        static readonly Dictionary<string, WorldDef> worlds = new Dictionary<string, WorldDef>();
        static readonly List<string> traitOrder = new List<string>();
        static readonly List<string> objectOrder = new List<string>();
        static readonly List<string> goalOrder = new List<string>();
        static readonly List<string> worldOrder = new List<string>();
        static bool loaded;

        public static string ModsPath => Path.Combine(Application.persistentDataPath, "Mods");

        public static void Load(bool force = false)
        {
            if (loaded && !force) return;
            traits.Clear(); objects.Clear(); goals.Clear(); worlds.Clear();
            traitOrder.Clear(); objectOrder.Clear(); goalOrder.Clear(); worldOrder.Clear();

            LoadFolder(Path.Combine(Application.streamingAssetsPath, "Content"));
            Directory.CreateDirectory(ModsPath);
            LoadFolder(ModsPath);
            Debug.Log("[OpenLife] Mods folder: " + ModsPath);
            Debug.Log($"[OpenLife] Content loaded: {objects.Count} objects, {traits.Count} traits, {goals.Count} goals, {worlds.Count} worlds");
            loaded = true;
        }

        static void LoadFolder(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[OpenLife] Content folder missing: " + dir);
                return;
            }
            var files = Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            foreach (var f in files)
            {
                try
                {
                    var cf = JsonUtility.FromJson<ContentFile>(File.ReadAllText(f));
                    if (cf != null) Merge(cf);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[OpenLife] Could not read {f}: {e.Message}");
                }
            }
        }

        static void Merge(ContentFile cf)
        {
            if (cf.traits != null) foreach (var t in cf.traits) Put(traits, traitOrder, t.id, t);
            if (cf.objects != null) foreach (var o in cf.objects) Put(objects, objectOrder, o.id, o);
            if (cf.goals != null) foreach (var g in cf.goals) Put(goals, goalOrder, g.id, g);
            if (cf.worlds != null) foreach (var w in cf.worlds) Put(worlds, worldOrder, w.id, w);
        }

        static void Put<T>(Dictionary<string, T> dict, List<string> order, string id, T item)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!dict.ContainsKey(id)) order.Add(id);
            dict[id] = item;
        }

        public static TraitDef GetTrait(string id) { traits.TryGetValue(id ?? "", out var t); return t; }
        public static ObjectDef GetObject(string id) { objects.TryGetValue(id ?? "", out var o); return o; }
        public static GoalDef GetGoal(string id) { goals.TryGetValue(id ?? "", out var g); return g; }
        public static WorldDef GetWorld(string id) { worlds.TryGetValue(id ?? "", out var w); return w; }

        public static List<TraitDef> AllTraits() { return Collect(traits, traitOrder); }
        public static List<ObjectDef> AllObjects() { return Collect(objects, objectOrder); }
        public static List<GoalDef> AllGoals() { return Collect(goals, goalOrder); }
        public static List<WorldDef> AllWorlds() { return Collect(worlds, worldOrder); }

        static List<T> Collect<T>(Dictionary<string, T> dict, List<string> order)
        {
            var list = new List<T>(order.Count);
            foreach (var id in order) list.Add(dict[id]);
            return list;
        }

        /// <summary>Distinct categories in first-seen order, Floor and Wall first.</summary>
        public static List<string> ObjectCategories()
        {
            var cats = new List<string>();
            foreach (var first in new[] { "Floor", "Wall" })
                foreach (var o in objects.Values) if (o.category == first && !cats.Contains(first)) cats.Add(first);
            foreach (var id in objectOrder)
            {
                var c = objects[id].category;
                if (!cats.Contains(c)) cats.Add(c);
            }
            return cats;
        }
    }
}
