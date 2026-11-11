using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    /// <summary>
    /// A living Sim: needs, traits, autonomy, relationships, and the walk/perform state machine.
    /// </summary>
    public class SimCharacter : MonoBehaviour
    {
        public enum State { Idle, Walking, Performing }

        class Job
        {
            public Interactable obj;
            public InteractionDef def;
            public SimCharacter partner;
            public bool social;
            public bool fromPartner;
        }

        // Base decay in need points per game hour, indexed by NeedType.
        static readonly float[] BaseDecayPerHour = { 5f, 4f, 4f, 3.5f, 3f, 6f };

        public static readonly InteractionDef ChatDef = new InteractionDef
        {
            id = "chat",
            name = "Chat",
            tag = "social",
            minutes = 30f,
            goalEvent = "chat",
            effects = new[]
            {
                new NeedAmount { need = "Social", amount = 45f },
                new NeedAmount { need = "Fun", amount = 10f }
            }
        };

        public SimData Data;
        public bool Autonomy = true;
        public string StatusText = "Idle";

        public State Current { get { return state; } }

        NavMeshAgent agent;
        Transform visual;
        readonly Queue<Job> queue = new Queue<Job>();
        Job job;
        State state = State.Idle;
        float progress;
        float thinkTimer;
        float bobT;
        TraitDef[] traits = new TraitDef[0];
        readonly float[] decayMult = { 1f, 1f, 1f, 1f, 1f, 1f };

        // ---------- setup ----------

        public void Init(SimData data, Transform visualRoot)
        {
            Data = data;
            visual = visualRoot;
            agent = GetComponent<NavMeshAgent>();
            if (Data.needs == null || Data.needs.Length != 6)
                Data.needs = new[] { 80f, 80f, 80f, 80f, 80f, 80f };
            RebuildTraits();
            thinkTimer = Random.value;
        }

        void RebuildTraits()
        {
            var list = new List<TraitDef>();
            if (Data.traitIds != null)
                foreach (var id in Data.traitIds)
                {
                    var t = ContentDatabase.GetTrait(id);
                    if (t != null) list.Add(t);
                }
            traits = list.ToArray();
            for (int i = 0; i < 6; i++) decayMult[i] = 1f;
            foreach (var t in traits)
            {
                if (t.decayMultipliers == null) continue;
                foreach (var m in t.decayMultipliers)
                {
                    int idx = Util.NeedIndex(m.need);
                    if (idx >= 0) decayMult[idx] *= m.amount;
                }
            }
        }

        void OnDestroy()
        {
            GameState.Sims.Remove(this);
            if (GameState.Selected == this) GameState.Selected = null;
        }

        // ---------- queries ----------

        public float Need(NeedType n) { return Data.needs[(int)n]; }

        public float AverageNeeds
        {
            get
            {
                float s = 0f;
                for (int i = 0; i < 6; i++) s += Data.needs[i];
                return s / 6f;
            }
        }

        public string Mood
        {
            get
            {
                float a = AverageNeeds;
                if (a >= 70f) return "Happy";
                if (a >= 45f) return "Fine";
                if (a >= 25f) return "Uncomfortable";
                return "Miserable";
            }
        }

        public NeedType Weakest
        {
            get
            {
                int w = 0;
                for (int i = 1; i < 6; i++) if (Data.needs[i] < Data.needs[w]) w = i;
                return (NeedType)w;
            }
        }

        public float Charm
        {
            get
            {
                float c = 1f;
                foreach (var t in traits) c *= t.charm;
                return c;
            }
        }

        public bool Likes(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            foreach (var t in traits)
                if (t.likes != null && System.Array.IndexOf(t.likes, tag) >= 0) return true;
            return false;
        }

        public bool Dislikes(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return false;
            foreach (var t in traits)
                if (t.dislikes != null && System.Array.IndexOf(t.dislikes, tag) >= 0) return true;
            return false;
        }

        public RelEntry GetRel(string otherName, bool create)
        {
            foreach (var r in Data.relationships) if (r.other == otherName) return r;
            if (!create) return null;
            var n = new RelEntry { other = otherName };
            Data.relationships.Add(n);
            return n;
        }

        // ---------- player commands ----------

        public void Enqueue(Interactable obj, InteractionDef def)
        {
            if (obj == null || def == null) return;
            queue.Enqueue(new Job { obj = obj, def = def });
        }

        public void EnqueueChat(SimCharacter other)
        {
            if (other == null || other == this) return;
            queue.Enqueue(new Job { social = true, partner = other, def = ChatDef });
        }

        public void ClearAll()
        {
            queue.Clear();
            CancelCurrent();
        }

        public int QueueLength { get { return queue.Count + (job != null ? 1 : 0); } }

        // ---------- main loop ----------

        void Update()
        {
            var clock = GameClock.I;
            if (clock == null || Data == null) return;
            float dm = clock.DeltaMinutes;

            DecayNeeds(dm);

            if (agent != null && agent.isOnNavMesh)
            {
                agent.speed = 2f * Mathf.Max(0.01f, clock.MoveSpeedFactor);
                agent.isStopped = clock.Speed == 0 || state != State.Walking;
            }

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.Walking: UpdateWalking(); break;
                case State.Performing: UpdatePerforming(dm); break;
            }

            Animate();
        }

        void DecayNeeds(float dm)
        {
            if (dm <= 0f) return;
            for (int i = 0; i < 6; i++)
            {
                float perMinute = BaseDecayPerHour[i] * decayMult[i] / 60f;
                Data.needs[i] = Mathf.Clamp(Data.needs[i] - perMinute * dm, 0f, 100f);
            }
        }

        void UpdateIdle()
        {
            StatusText = "Idle";
            if (queue.Count > 0)
            {
                StartJob(queue.Dequeue());
                return;
            }
            if (!Autonomy) return;
            thinkTimer -= Time.deltaTime;
            if (thinkTimer <= 0f)
            {
                thinkTimer = 1f + Random.value;
                Think();
            }
        }

        void StartJob(Job j)
        {
            job = j;
            progress = 0f;

            if (j.fromPartner) { BeginPerform(); return; }

            if (j.social)
            {
                if (j.partner == null || j.partner == this) { job = null; return; }
                state = State.Walking;
                StatusText = "Going to chat with " + j.partner.Data.name;
                SetDestination(j.partner.transform.position);
                return;
            }

            if (j.obj == null) { job = null; return; }
            if (j.obj.User != null && j.obj.User != this)
            {
                GameState.Notify(j.obj.Def.name + " is in use.");
                job = null;
                return;
            }
            j.obj.User = this;
            state = State.Walking;
            StatusText = "Heading to " + j.obj.Def.name;
            SetDestination(j.obj.UsePoint());
        }

        void SetDestination(Vector3 p)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(p);
            }
        }

        void UpdateWalking()
        {
            if (job == null) { state = State.Idle; return; }

            if (job.social)
            {
                if (job.partner == null) { CancelCurrent(); return; }
                float dist = Vector3.Distance(transform.position, job.partner.transform.position);
                if (Time.frameCount % 20 == 0) SetDestination(job.partner.transform.position);
                if (dist < 1.8f)
                {
                    if (job.partner.TryJoinChat(this)) BeginPerform();
                    else
                    {
                        GameState.Notify(job.partner.Data.name + " is busy.");
                        CancelCurrent();
                    }
                }
                return;
            }

            if (job.obj == null) { CancelCurrent(); return; }
            if (agent == null || !agent.isOnNavMesh) return;
            if (agent.pathPending) return;

            bool arrived = agent.remainingDistance <= agent.stoppingDistance + 0.2f;
            if (agent.pathStatus != NavMeshPathStatus.PathComplete && arrived)
            {
                float d = Vector3.Distance(transform.position, job.obj.UsePoint());
                if (d > 1.5f)
                {
                    GameState.Notify(Data.name + " can't reach the " + job.obj.Def.name + ".");
                    CancelCurrent();
                    return;
                }
            }
            if (arrived) BeginPerform();
        }

        void BeginPerform()
        {
            state = State.Performing;
            progress = 0f;
            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        void UpdatePerforming(float dm)
        {
            if (job == null) { state = State.Idle; return; }
            var def = job.def;
            StatusText = def.name;

            if (job.social)
            {
                if (job.partner == null) { CancelCurrent(); return; }
                FaceTowards(job.partner.transform.position);
            }
            else if (job.obj != null)
            {
                FaceTowards(job.obj.transform.position);
            }

            float frac = def.minutes > 0f ? dm / def.minutes : 1f;
            progress += dm;
            if (def.effects != null)
            {
                foreach (var e in def.effects)
                {
                    int idx = Util.NeedIndex(e.need);
                    if (idx < 0) continue;
                    Data.needs[idx] = Mathf.Clamp(Data.needs[idx] + e.amount * frac, 0f, 100f);
                }
            }

            if (progress >= def.minutes) FinishJob();
        }

        void FinishJob()
        {
            var d = job.def;
            if (d.money != 0)
            {
                GameState.Funds += d.money;
                if (d.money > 0) GameState.Notify(Data.name + " earned $" + d.money);
            }

            if (job.social)
            {
                if (!job.fromPartner)
                {
                    GoalSystem.Fire(d.goalEvent);
                    if (job.partner != null) CompleteChat(this, job.partner);
                }
            }
            else
            {
                GoalSystem.Fire(d.goalEvent);
            }

            ReleaseObject();
            job = null;
            state = State.Idle;
        }

        void ReleaseObject()
        {
            if (job != null && job.obj != null && job.obj.User == this) job.obj.User = null;
        }

        void CancelCurrent()
        {
            ReleaseObject();
            job = null;
            state = State.Idle;
            if (agent != null && agent.isOnNavMesh) agent.ResetPath();
        }

        // ---------- social ----------

        public bool TryJoinChat(SimCharacter other)
        {
            if (state == State.Performing && job != null)
            {
                if (job.social && job.partner == other) return true;
                if (!job.social) return false;
            }
            CancelCurrent();
            job = new Job { social = true, fromPartner = true, partner = other, def = ChatDef };
            BeginPerform();
            return true;
        }

        static void CompleteChat(SimCharacter a, SimCharacter b)
        {
            float gain = 6f * a.Charm * b.Charm;
            if (a.Likes("social")) gain += 2f;
            if (b.Likes("social")) gain += 2f;
            if (a.Dislikes("social")) gain -= 2f;
            if (b.Dislikes("social")) gain -= 2f;
            gain = Mathf.Max(1f, gain);
            a.AddFriendship(b, gain, true);
            b.AddFriendship(a, gain, false);
        }

        void AddFriendship(SimCharacter other, float gain, bool announce)
        {
            var r = GetRel(other.Data.name, true);
            r.friendship = Mathf.Min(100f, r.friendship + gain);
            if (!r.friends && r.friendship >= 30f)
            {
                r.friends = true;
                if (announce)
                {
                    GameState.Notify(Data.name + " and " + other.Data.name + " are now friends!");
                    GoalSystem.Fire("friend_made");
                }
            }
        }

        // ---------- autonomy ----------

        void Think()
        {
            Interactable bestObj = null;
            InteractionDef bestDef = null;
            SimCharacter bestSim = null;
            float best = 0.6f;

            foreach (var it in Interactable.All)
            {
                if (it == null || it.Def == null || it.Def.interactions == null) continue;
                if (it.User != null && it.User != this) continue;
                float dist = Vector3.Distance(transform.position, it.transform.position);
                foreach (var def in it.Def.interactions)
                {
                    float s = Score(def) - dist * 0.04f + Random.value * 0.4f;
                    if (s > best) { best = s; bestObj = it; bestDef = def; bestSim = null; }
                }
            }

            foreach (var o in GameState.Sims)
            {
                if (o == null || o == this) continue;
                if (o.state == State.Performing && o.job != null && !o.job.social) continue;
                float dist = Vector3.Distance(transform.position, o.transform.position);
                float s = Score(ChatDef) - dist * 0.04f + Random.value * 0.4f;
                if (s > best) { best = s; bestSim = o; bestObj = null; bestDef = null; }
            }

            if (bestSim != null) queue.Enqueue(new Job { social = true, partner = bestSim, def = ChatDef });
            else if (bestObj != null) queue.Enqueue(new Job { obj = bestObj, def = bestDef });
        }

        float Score(InteractionDef def)
        {
            float s = 0f;
            if (def.effects != null)
            {
                foreach (var e in def.effects)
                {
                    int idx = Util.NeedIndex(e.need);
                    if (idx < 0) continue;
                    float need = Data.needs[idx];
                    float deficit = 1f - need / 100f;
                    if (e.amount > 0f)
                    {
                        float usable = Mathf.Min(e.amount, 100f - need) / 100f;
                        s += usable * deficit * 8f;
                        if (need < 25f) s += e.amount * 0.05f;
                    }
                    else
                    {
                        s += e.amount * 0.02f * (need < 30f ? 2f : 1f);
                    }
                }
            }
            if (Likes(def.tag)) s += 0.8f;
            if (Dislikes(def.tag)) s -= 0.8f;
            if (def.money > 0 && GameState.Funds < 1500) s += 1f + def.money * 0.005f;
            return s;
        }

        // ---------- visuals ----------

        void FaceTowards(Vector3 p)
        {
            Vector3 dir = p - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
        }

        void Animate()
        {
            if (visual == null) return;

            bool sleeping = state == State.Performing && job != null && job.def != null && job.def.tag == "sleep";
            if (sleeping)
            {
                visual.localRotation = Quaternion.Euler(85f, 0f, 0f);
                visual.localPosition = new Vector3(0f, 0.6f, 0f);
                return;
            }

            visual.localRotation = Quaternion.identity;
            float speed = (agent != null && agent.isOnNavMesh) ? agent.velocity.magnitude : 0f;
            bobT += Time.deltaTime * speed * 3f;
            visual.localPosition = new Vector3(0f, Mathf.Abs(Mathf.Sin(bobT)) * 0.06f, 0f);
        }

        // ---------- save helpers ----------

        public void StorePosition()
        {
            Data.x = transform.position.x;
            Data.y = transform.position.y;
            Data.z = transform.position.z;
        }
    }
}
