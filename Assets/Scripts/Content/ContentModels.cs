using System;
using UnityEngine;

namespace OpenLife
{
    public enum NeedType { Hunger = 0, Energy = 1, Fun = 2, Social = 3, Hygiene = 4, Bladder = 5 }

    [Serializable]
    public class NeedAmount
    {
        public string need;
        public float amount;
    }

    [Serializable]
    public class TraitDef
    {
        public string id;
        public string name;
        public string description;
        public NeedAmount[] decayMultipliers;
        public string[] likes;
        public string[] dislikes;
        public float charm = 1f;
    }

    [Serializable]
    public class InteractionDef
    {
        public string id;
        public string name;
        public string tag;
        public float minutes = 30f;
        public NeedAmount[] effects;
        public int money;
        public string goalEvent;
    }

    [Serializable]
    public class ObjectDef
    {
        public string id;
        public string name;
        public string category = "Furniture";
        public int price = 10;
        public int sizeX = 1;
        public int sizeZ = 1;
        public float height = 1f;
        public string shape = "box";
        public string color = "#cccccc";
        public string prefab;
        public InteractionDef[] interactions;

        public bool IsFloor => category == "Floor";
        public bool IsWall => category == "Wall";
    }

    [Serializable]
    public class GoalDef
    {
        public string id;
        public string title;
        public string eventId;
        public int target = 1;
        public int reward;
    }

    [Serializable]
    public class ScatterDef
    {
        public string shape = "tree";
        public string color = "#3f8f3a";
        public float density = 0.2f;
        public float minScale = 0.8f;
        public float maxScale = 1.4f;
        public float height = 4f;
        public float width = 0.6f;
    }

    [Serializable]
    public class WorldDef
    {
        public string id;
        public string name;
        public string description;
        public string groundColor = "#5f9f4a";
        public string skyColor = "#8fc8ee";
        public string sunColor = "#fff2d6";
        public float sunIntensity = 1.1f;
        public float fog;
        public int size = 80;
        public int startHour = 8;
        public ScatterDef[] scatter;
    }

    [Serializable]
    public class ContentFile
    {
        public TraitDef[] traits;
        public ObjectDef[] objects;
        public GoalDef[] goals;
        public WorldDef[] worlds;
    }

    public static class Util
    {
        public static Color ParseColor(string hex, Color fallback)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return fallback;
        }

        public static string ToHex(Color c)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(c);
        }

        public static int NeedIndex(string name)
        {
            if (!string.IsNullOrEmpty(name) && Enum.TryParse<NeedType>(name, true, out var n)) return (int)n;
            return -1;
        }
    }
}
