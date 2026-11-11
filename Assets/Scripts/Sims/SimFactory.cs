using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    public static class Palettes
    {
        public static readonly string[] SkinTones =
        {
            "#f1c9a5", "#e0ac84", "#c68642", "#a8693b", "#8d5524", "#6b3e1f", "#4a2912", "#2f1b0c"
        };

        public static readonly string[] HairColors =
        {
            "#0d0d0d", "#2b1a10", "#5a3825", "#8b5a2b", "#c9a063", "#d8d8d8", "#7a1f3d", "#2f4f9f"
        };

        public static readonly string[] OutfitColors =
        {
            "#7a1f3d", "#2f6f4f", "#2f4f9f", "#c9a063", "#d06f3f", "#5a4a8f", "#2b2b2b", "#e8e2d6"
        };

        public static readonly string[] HairStyles = { "braids", "bun", "afro", "short", "long", "bald" };

        public static readonly string[] GroundColors =
        {
            "#5f9f4a", "#c9a85a", "#eef2f7", "#e0c080", "#2a2a35", "#7a5a3a"
        };

        public static readonly string[] SkyColors =
        {
            "#8fc8ee", "#6fd0f0", "#b8cde0", "#f0a36b", "#1a1030", "#c9b8e8"
        };

        public static readonly string[] BuildColors =
        {
            "", "#e8e2d6", "#d06f6f", "#d0a24a", "#5fa05a", "#4f7fd0", "#8a5a9f", "#3c4048"
        };
    }

    /// <summary>
    /// Builds a Sim from primitive shapes. To use real art, replace this method with one
    /// that instantiates your character prefab and calls SimCharacter.Init on it.
    /// </summary>
    public static class SimFactory
    {
        public const int SimLayer = 4;

        public static SimCharacter Create(SimData d, Vector3 position)
        {
            var root = new GameObject("Sim_" + d.name);
            root.SetActive(false);
            root.transform.position = position;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            BuildBody(visual, d);

            var col = root.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 0.95f, 0f);
            col.radius = 0.3f;
            col.height = 1.9f;

            var agent = root.AddComponent<NavMeshAgent>();
            agent.agentTypeID = NavSetup.AgentTypeId;
            agent.radius = 0.25f;
            agent.height = 1.8f;
            agent.acceleration = 14f;
            agent.angularSpeed = 540f;
            agent.stoppingDistance = 0.1f;

            var filter = new NavMeshQueryFilter { agentTypeID = NavSetup.AgentTypeId, areaMask = NavMesh.AllAreas };
            if (NavMesh.SamplePosition(position, out var hit, 4f, filter))
                root.transform.position = hit.position;

            var sim = root.AddComponent<SimCharacter>();
            sim.Init(d, visual);
            // Layer 4 ("Water") is unused here; the navmesh build skips it so Sims don't block paths.
            ObjectFactory.SetLayer(root, SimLayer);
            root.SetActive(true);
            GameState.Sims.Add(sim);
            return sim;
        }

        static void BuildBody(Transform vis, SimData d)
        {
            Color skin = Util.ParseColor(d.skinHex, new Color(0.55f, 0.33f, 0.14f));
            Color hair = Util.ParseColor(d.hairHex, Color.black);
            Color outfit = Util.ParseColor(d.outfitHex, Color.gray);
            Color pants = new Color(0.18f, 0.23f, 0.33f);

            Part(vis, PrimitiveType.Capsule, "LegL", new Vector3(-0.11f, 0.4f, 0f), new Vector3(0.2f, 0.4f, 0.2f), pants);
            Part(vis, PrimitiveType.Capsule, "LegR", new Vector3(0.11f, 0.4f, 0f), new Vector3(0.2f, 0.4f, 0.2f), pants);
            Part(vis, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.12f, 0f), new Vector3(0.5f, 0.38f, 0.33f), outfit);
            Part(vis, PrimitiveType.Capsule, "ArmL", new Vector3(-0.31f, 1.12f, 0f), new Vector3(0.14f, 0.34f, 0.14f), skin);
            Part(vis, PrimitiveType.Capsule, "ArmR", new Vector3(0.31f, 1.12f, 0f), new Vector3(0.14f, 0.34f, 0.14f), skin);
            Part(vis, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.76f, 0f), Vector3.one * 0.34f, skin);

            switch (d.hairStyle)
            {
                case "bald":
                    break;
                case "afro":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.86f, -0.02f), Vector3.one * 0.5f, hair);
                    break;
                case "short":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.84f, -0.01f), new Vector3(0.37f, 0.24f, 0.37f), hair);
                    break;
                case "long":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.84f, -0.01f), new Vector3(0.37f, 0.24f, 0.37f), hair);
                    Part(vis, PrimitiveType.Capsule, "HairBack", new Vector3(0f, 1.58f, -0.13f), new Vector3(0.3f, 0.36f, 0.12f), hair);
                    break;
                case "bun":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.84f, -0.01f), new Vector3(0.37f, 0.24f, 0.37f), hair);
                    Part(vis, PrimitiveType.Sphere, "Bun", new Vector3(0f, 2.0f, -0.06f), Vector3.one * 0.17f, hair);
                    break;
                default: // braids
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.84f, -0.01f), new Vector3(0.37f, 0.24f, 0.37f), hair);
                    Part(vis, PrimitiveType.Sphere, "Bun", new Vector3(0f, 2.0f, -0.06f), Vector3.one * 0.18f, hair);
                    for (int i = 0; i < 5; i++)
                    {
                        float x = -0.1f + i * 0.05f;
                        Part(vis, PrimitiveType.Capsule, "Braid" + i, new Vector3(x, 1.62f, -0.15f), new Vector3(0.05f, 0.3f, 0.05f), hair);
                    }
                    break;
            }
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialUtil.Shared(color);
        }
    }
}
