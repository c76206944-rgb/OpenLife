using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    public static class Palettes
    {
        public static readonly string[] SkinTones = { "#f1c9a5", "#e0ac84", "#c68642", "#a8693b", "#8d5524", "#6b3e1f", "#4a2912", "#2f1b0c" };
        public static readonly string[] HairColors = { "#0d0d0d", "#2b1a10", "#5a3825", "#8b5a2b", "#c9a063", "#d8d8d8", "#7a1f3d", "#2f4f9f" };
        public static readonly string[] OutfitColors = { "#7a1f3d", "#2f6f4f", "#2f4f9f", "#c9a063", "#d06f3f", "#5a4a8f", "#2b2b2b", "#e8e2d6" };
        public static readonly string[] HairStyles = { "braids", "bun", "afro", "short", "long", "bald" };
        public static readonly string[] GroundColors = { "#5f9f4a", "#c9a85a", "#eef2f7", "#e0c080", "#2a2a35", "#7a5a3a" };
        public static readonly string[] SkyColors = { "#8fc8ee", "#6fd0f0", "#b8cde0", "#f0a36b", "#1a1030", "#c9b8e8" };
        public static readonly string[] BuildColors = { "", "#e8e2d6", "#d06f6f", "#d0a24a", "#5fa05a", "#4f7fd0", "#8a5a9f", "#3c4048" };
    }

    /// <summary>
    /// Creates the gameplay Sim root and attaches a replaceable visual prefab.
    /// Put rigged models in Assets/Resources/Characters and set SimData.modelId
    /// to the Resources-relative path (without extension). The fallback remains
    /// available so existing saves and character creation continue to work.
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
            if (!TryBuildPrefab(visual, d)) BuildBody(visual, d);

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
            root.AddComponent<SimAnimationDriver>().Init(visual, agent, sim);
            ObjectFactory.SetLayer(root, SimLayer);
            root.SetActive(true);
            GameState.Sims.Add(sim);
            return sim;
        }

        static bool TryBuildPrefab(Transform visual, SimData data)
        {
            if (string.IsNullOrWhiteSpace(data.modelId)) return false;
            var prefab = Resources.Load<GameObject>(data.modelId);
            if (prefab == null)
            {
                Debug.LogWarning("OpenLife character prefab not found at Resources/" + data.modelId + ". Using fallback avatar.");
                return false;
            }

            var instance = Object.Instantiate(prefab, visual, false);
            instance.name = "CharacterModel";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            return true;
        }

        static void BuildBody(Transform vis, SimData d)
        {
            Color skin = Util.ParseColor(d.skinHex, new Color(0.55f, 0.33f, 0.14f));
            Color hair = Util.ParseColor(d.hairHex, Color.black);
            Color outfit = Util.ParseColor(d.outfitHex, Color.gray);
            Color pants = new Color(0.13f, 0.17f, 0.25f);
            Color shoe = new Color(0.08f, 0.08f, 0.1f);
            Color detail = Color.Lerp(outfit, Color.white, 0.28f);

            // Feet, tapered-looking legs, layered clothing and hands.
            Part(vis, PrimitiveType.Cube, "ShoeL", new Vector3(-0.12f, 0.09f, 0.07f), new Vector3(0.17f, 0.13f, 0.31f), shoe);
            Part(vis, PrimitiveType.Cube, "ShoeR", new Vector3(0.12f, 0.09f, 0.07f), new Vector3(0.17f, 0.13f, 0.31f), shoe);
            Part(vis, PrimitiveType.Capsule, "LegL", new Vector3(-0.12f, 0.43f, 0f), new Vector3(0.19f, 0.43f, 0.19f), pants);
            Part(vis, PrimitiveType.Capsule, "LegR", new Vector3(0.12f, 0.43f, 0f), new Vector3(0.19f, 0.43f, 0.19f), pants);
            Part(vis, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.12f, 0f), new Vector3(0.49f, 0.39f, 0.34f), outfit);
            Part(vis, PrimitiveType.Sphere, "Collar", new Vector3(0f, 1.42f, 0.015f), new Vector3(0.22f, 0.1f, 0.19f), detail);
            Part(vis, PrimitiveType.Capsule, "ArmL", new Vector3(-0.32f, 1.12f, 0f), new Vector3(0.14f, 0.34f, 0.14f), outfit);
            Part(vis, PrimitiveType.Capsule, "ArmR", new Vector3(0.32f, 1.12f, 0f), new Vector3(0.14f, 0.34f, 0.14f), outfit);
            Part(vis, PrimitiveType.Sphere, "HandL", new Vector3(-0.33f, 0.78f, 0.01f), new Vector3(0.12f, 0.14f, 0.11f), skin);
            Part(vis, PrimitiveType.Sphere, "HandR", new Vector3(0.33f, 0.78f, 0.01f), new Vector3(0.12f, 0.14f, 0.11f), skin);
            Part(vis, PrimitiveType.Cylinder, "Neck", new Vector3(0f, 1.49f, 0f), new Vector3(0.105f, 0.13f, 0.105f), skin);
            Part(vis, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.76f, 0f), new Vector3(0.29f, 0.36f, 0.28f), skin);

            // Face is oriented toward local +Z.
            Part(vis, PrimitiveType.Sphere, "EarL", new Vector3(-0.285f, 1.76f, 0f), new Vector3(0.075f, 0.11f, 0.08f), skin);
            Part(vis, PrimitiveType.Sphere, "EarR", new Vector3(0.285f, 1.76f, 0f), new Vector3(0.075f, 0.11f, 0.08f), skin);
            Color white = new Color(0.94f, 0.92f, 0.86f);
            Color iris = new Color(0.18f, 0.11f, 0.07f);
            Part(vis, PrimitiveType.Sphere, "EyeWhiteL", new Vector3(-0.105f, 1.81f, 0.245f), new Vector3(0.085f, 0.055f, 0.035f), white);
            Part(vis, PrimitiveType.Sphere, "EyeWhiteR", new Vector3(0.105f, 1.81f, 0.245f), new Vector3(0.085f, 0.055f, 0.035f), white);
            Part(vis, PrimitiveType.Sphere, "IrisL", new Vector3(-0.105f, 1.81f, 0.277f), new Vector3(0.033f, 0.04f, 0.018f), iris);
            Part(vis, PrimitiveType.Sphere, "IrisR", new Vector3(0.105f, 1.81f, 0.277f), new Vector3(0.033f, 0.04f, 0.018f), iris);
            Part(vis, PrimitiveType.Sphere, "Nose", new Vector3(0f, 1.73f, 0.27f), new Vector3(0.055f, 0.08f, 0.07f), skin);
            Part(vis, PrimitiveType.Sphere, "Mouth", new Vector3(0f, 1.65f, 0.253f), new Vector3(0.075f, 0.018f, 0.025f), new Color(0.34f, 0.12f, 0.12f));

            switch ((d.hairStyle ?? "short").ToLowerInvariant())
            {
                case "bald": break;
                case "afro":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.9f, -0.02f), new Vector3(0.36f, 0.28f, 0.35f), hair);
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * Mathf.PI * 2f / 10f;
                        Part(vis, PrimitiveType.Sphere, "Curl" + i, new Vector3(Mathf.Cos(a) * 0.27f, 1.88f, Mathf.Sin(a) * 0.25f), Vector3.one * 0.12f, hair);
                    }
                    break;
                case "short":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.94f, -0.01f), new Vector3(0.31f, 0.17f, 0.31f), hair);
                    break;
                case "long":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.94f, -0.01f), new Vector3(0.32f, 0.17f, 0.32f), hair);
                    Part(vis, PrimitiveType.Capsule, "HairBack", new Vector3(0f, 1.57f, -0.16f), new Vector3(0.29f, 0.4f, 0.12f), hair);
                    Part(vis, PrimitiveType.Capsule, "HairLeft", new Vector3(-0.25f, 1.65f, 0.02f), new Vector3(0.08f, 0.3f, 0.09f), hair);
                    Part(vis, PrimitiveType.Capsule, "HairRight", new Vector3(0.25f, 1.65f, 0.02f), new Vector3(0.08f, 0.3f, 0.09f), hair);
                    break;
                case "bun":
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.94f, -0.01f), new Vector3(0.32f, 0.17f, 0.32f), hair);
                    Part(vis, PrimitiveType.Sphere, "Bun", new Vector3(0f, 2.08f, -0.12f), Vector3.one * 0.16f, hair);
                    break;
                default: // braids
                    Part(vis, PrimitiveType.Sphere, "Hair", new Vector3(0f, 1.94f, -0.01f), new Vector3(0.32f, 0.17f, 0.32f), hair);
                    for (int i = 0; i < 7; i++)
                    {
                        float x = -0.18f + i * 0.06f;
                        Part(vis, PrimitiveType.Capsule, "Braid" + i, new Vector3(x, 1.58f, -0.12f), new Vector3(0.045f, 0.31f, 0.045f), hair);
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
