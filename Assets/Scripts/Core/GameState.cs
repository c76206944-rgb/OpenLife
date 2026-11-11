using System;
using System.Collections.Generic;

namespace OpenLife
{
    /// <summary>Shared runtime state for the current game.</summary>
    public static class GameState
    {
        public const int StartingFunds = 20000;

        public static int Funds = StartingFunds;
        public static readonly List<SimCharacter> Sims = new List<SimCharacter>();
        public static SimCharacter Selected;
        public static WorldSettings World = new WorldSettings();

        public static event Action<string> ToastRaised;

        public static void Notify(string message)
        {
            if (ToastRaised != null) ToastRaised(message);
        }

        public static void Reset()
        {
            Funds = StartingFunds;
            Sims.Clear();
            Selected = null;
        }

        public static void Select(SimCharacter sim)
        {
            Selected = sim;
        }
    }
}
