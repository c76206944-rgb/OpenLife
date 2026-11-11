using UnityEngine;

namespace OpenLife
{
    /// <summary>Game time. Speed 0 = paused, 1..3 = faster.</summary>
    [DefaultExecutionOrder(-100)]
    public class GameClock : MonoBehaviour
    {
        public static GameClock I;

        // game minutes that pass per real second at each speed
        static readonly float[] MinutesPerSecond = { 0f, 2f, 8f, 30f };
        // movement speed multiplier at each speed
        static readonly float[] MoveFactor = { 0f, 1f, 2.5f, 5f };
        static readonly string[] DayNames = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        public float TotalMinutes;
        public int Speed = 1;
        public float DeltaMinutes { get; private set; }
        public float MoveSpeedFactor => MoveFactor[Mathf.Clamp(Speed, 0, 3)];

        public int Day => (int)(TotalMinutes / 1440f);
        public int Hour => (int)(TotalMinutes % 1440f / 60f);
        public int Minute => (int)(TotalMinutes % 60f);
        public float HourFloat => TotalMinutes % 1440f / 60f;

        public string Label
        {
            get
            {
                int h = Hour % 12; if (h == 0) h = 12;
                string ap = Hour < 12 ? "AM" : "PM";
                return $"{DayNames[Day % 7]} {h}:{Minute:00} {ap}";
            }
        }

        void Awake() { I = this; }

        void Update()
        {
            DeltaMinutes = Time.deltaTime * MinutesPerSecond[Mathf.Clamp(Speed, 0, 3)];
            TotalMinutes += DeltaMinutes;
        }

        public void SetSpeed(int s) { Speed = Mathf.Clamp(s, 0, 3); }
        public void SetTime(int day, float hour) { TotalMinutes = day * 1440f + hour * 60f; }
    }
}
