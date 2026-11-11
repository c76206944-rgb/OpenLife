using System;
using System.Collections.Generic;

namespace OpenLife
{
    [Serializable]
    public class RelEntry
    {
        public string other;
        public float friendship;
        public bool friends;
    }

    /// <summary>Everything about a Sim that gets saved.</summary>
    [Serializable]
    public class SimData
    {
        public string name = "Sim";
        public string skinHex = "#8d5524";
        public string hairHex = "#1b1b1b";
        public string outfitHex = "#7a1f3d";
        public string hairStyle = "bun";
        public string[] traitIds = new string[0];
        public float[] needs = { 80f, 80f, 80f, 80f, 80f, 80f };
        public float x, y, z;
        public List<RelEntry> relationships = new List<RelEntry>();
    }

    /// <summary>The player's customization of a world preset.</summary>
    [Serializable]
    public class WorldSettings
    {
        public string worldId = "green_campus";
        public string displayName = "My World";
        public string groundHex = "";
        public string skyHex = "";
        public float propScale = 1f;
        public int startHour = 8;
        public int seed = 12345;
        public bool emptyLot;
    }
}
