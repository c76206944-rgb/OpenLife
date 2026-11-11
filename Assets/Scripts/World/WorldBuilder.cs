using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    /// <summary>Builds the ground, sky, sun, scattered props and the navmesh for the chosen setting.</summary>
    public class WorldBuilder : MonoBehaviour
    {
        /// <summary>The buildable area spans cells -GridHalf .. GridHalf-1 on both axes.</summary>
        public const int GridHalf = 16;

        public Transform BuildRoot { get; private set; }

        Transform groundRoot;
        Transform propRoot;
        Light sun;
        Camera cam;
        NavMeshData navData;
        NavMeshDataInstance navInstance;
        bool navReady;
        WorldDef def;
        Color skyDay = new Color(0.56f, 0.78f, 0.93f);
        Color skyNight = new Color(0.03f, 0.04f, 0.1f);
        Color sunColor = Color.white;
        float sunBase = 1.1f;
        float fogDensity;

        public void Build(WorldSettings ws, Camera camera)
        {
            cam = camera;
            Clear();

            def = ContentDatabase.GetWorld(ws.worldId);
            if (def == null)
            {
                var all = ContentDatabase.AllWorlds();
                def = all.Count > 0 ? all[0] : new WorldDef { id = "default", name = "Default" };
            }

            Color ground = Util.ParseColor(string.IsNullOrEmpty(ws.groundHex) ? def.groundColor : ws.groundHex, new Color(0.37f, 0.62f, 0.29f));
            skyDay = Util.ParseColor(string.IsNullOrEmpty(ws.skyHex) ? def.skyColor : ws.skyHex, skyDay);
            sunColor = Util.ParseColor(def.sunColor, Color.white);
            sunBase = def.sunIntensity;
            fogDensity = def.fog;

            groundRoot = new GameObject("Ground").transform;
            groundRoot.SetParent(transform, false);
            propRoot = new GameObject("Props").transform;
            propRoot.SetParent(transform, false);
            BuildRoot = new GameObject("BuildRoot").transform;
            BuildRoot.SetParent(transform, false);

            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = "GroundPlane";
            g.transform.SetParent(groundRoot, false);
            g.transform.localScale = new Vector3(def.size, 0.4f, def.size);
            g.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            g.GetComponent<Renderer>().sharedMaterial = MaterialUtil.Shared(ground);

            SetupLight();
            SetupCamera();
            Scatter(ws);
            Rebake();
        }

        void Clear()
        {
            // Deactivate first: Destroy is deferred, and inactive objects are skipped by the navmesh build.
            foreach (Transform c in transform)
            {
                c.gameObject.SetActive(false);
                Destroy(c.gameObject);
            }
        }

        void SetupLight()
        {
            if (sun == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) Destroy(l.gameObject);
                var go = new GameObject("Sun");
                go.transform.SetParent(transform, false);
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.shadows = LightShadows.Soft;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.fog = fogDensity > 0f;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = fogDensity;
        }

        void SetupCamera()
        {
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = skyDay;
            cam.farClipPlane = 400f;
        }

        /// <summary>
        /// Rebuilds the navigation mesh from physics colliders inside the build area.
        /// Walls and furniture are marked Not Walkable; floors and doorways stay walkable.
        /// </summary>
        public void Rebake()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(GridHalf * 2 + 2, 10f, GridHalf * 2 + 2));
            var markups = new List<NavMeshBuildMarkup>();
            if (BuildRoot != null)
            {
                foreach (var po in BuildRoot.GetComponentsInChildren<PlacedObject>())
                {
                    if (po.Def == null || po.Def.IsFloor || po.Def.shape == "lintel") continue;
                    markups.Add(new NavMeshBuildMarkup
                    {
                        root = po.transform,
                        overrideArea = true,
                        area = 1, // Not Walkable
                        ignoreFromBuild = false,
                        applyToChildren = true
                    });
                }
            }

            Physics.SyncTransforms();

            // Skip layer 2 (ghost preview) and layer 4 (Sims).
            int mask = ~((1 << 2) | (1 << 4));
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, mask, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            var settings = NavSetup.Settings;
            if (!navReady)
            {
                navData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
                navInstance = NavMesh.AddNavMeshData(navData);
                navReady = true;
            }
            else
            {
                NavMeshBuilder.UpdateNavMeshData(navData, settings, sources, bounds);
            }
        }

        // ---------- scatter props ----------

        void Scatter(WorldSettings ws)
        {
            if (def.scatter == null) return;
            var rng = new System.Random(ws.seed);
            float half = def.size * 0.5f;
            float clear = GridHalf + 4f;

            foreach (var s in def.scatter)
            {
                float area = def.size * def.size - (clear * 2f) * (clear * 2f);
                int count = Mathf.RoundToInt(Mathf.Max(0f, s.density * ws.propScale * area / 100f));
                Color baseColor = Util.ParseColor(s.color, Color.green);
                for (int i = 0; i < count; i++)
                {
                    float x = (float)(rng.NextDouble() * 2.0 - 1.0) * (half - 1f);
                    float z = (float)(rng.NextDouble() * 2.0 - 1.0) * (half - 1f);
                    if (Mathf.Abs(x) < clear && Mathf.Abs(z) < clear) continue;
                    float scale = Mathf.Lerp(s.minScale, s.maxScale, (float)rng.NextDouble());
                    float shade = 0.9f + 0.1f * rng.Next(0, 3);
                    Color c = new Color(baseColor.r * shade, baseColor.g * shade, baseColor.b * shade, 1f);
                    MakeProp(s, c, scale, new Vector3(x, 0f, z), (float)rng.NextDouble() * 360f);
                }
            }
        }

        void MakeProp(ScatterDef s, Color c, float scale, Vector3 pos, float yaw)
        {
            var root = new GameObject(s.shape).transform;
            root.SetParent(propRoot, false);
            root.position = pos;
            root.rotation = Quaternion.Euler(0f, yaw, 0f);

            Color trunk = new Color(0.35f, 0.24f, 0.14f);
            float H = s.height * scale;
            float W = s.width * scale;

            switch (s.shape)
            {
                case "pine":
                    Piece(root, PrimitiveType.Cylinder, new Vector3(0f, H * 0.15f, 0f), new Vector3(W * 0.4f, H * 0.15f, W * 0.4f), trunk);
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, H * 0.55f, 0f), new Vector3(H * 0.3f, H * 0.8f, H * 0.3f), c);
                    break;
                case "palm":
                    Piece(root, PrimitiveType.Cylinder, new Vector3(0f, H * 0.5f, 0f), new Vector3(W * 0.5f, H * 0.5f, W * 0.5f), trunk);
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, H, 0f), new Vector3(H * 0.45f, H * 0.08f, H * 0.45f), c);
                    break;
                case "rock":
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, H * 0.25f, 0f), new Vector3(W, H * 0.6f, W * 0.9f), c);
                    break;
                case "cactus":
                    Piece(root, PrimitiveType.Capsule, new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H * 0.5f, W), c);
                    break;
                case "tower":
                    Piece(root, PrimitiveType.Cube, new Vector3(0f, H * 0.5f, 0f), new Vector3(W, H, W), c);
                    break;
                default: // tree
                    float trunkH = H * 0.45f;
                    Piece(root, PrimitiveType.Cylinder, new Vector3(0f, trunkH * 0.5f, 0f), new Vector3(W * 0.4f, trunkH * 0.5f, W * 0.4f), trunk);
                    float crown = H * 0.55f * Mathf.Max(0.6f, s.width / 0.6f);
                    Piece(root, PrimitiveType.Sphere, new Vector3(0f, trunkH + crown * 0.3f, 0f), new Vector3(crown, crown * 0.9f, crown), c);
                    break;
            }
        }

        static void Piece(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Color c)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = MaterialUtil.Shared(c);
        }

        // ---------- day and night ----------

        void Update()
        {
            var clock = GameClock.I;
            if (clock == null || sun == null) return;

            float hour = clock.HourFloat;
            float angle = (hour - 6f) / 24f * 360f;
            sun.transform.rotation = Quaternion.Euler(angle, -30f, 0f);

            float elevation = Mathf.Sin(angle * Mathf.Deg2Rad);
            float day = Mathf.Clamp01(elevation * 2f + 0.25f);

            sun.intensity = Mathf.Lerp(0.1f, sunBase, day);
            sun.color = Color.Lerp(new Color(0.5f, 0.55f, 1f), sunColor, day);

            Color sky = Color.Lerp(skyNight, skyDay, day);
            if (cam != null) cam.backgroundColor = sky;
            RenderSettings.fogColor = sky;
            RenderSettings.ambientLight = Color.Lerp(new Color(0.08f, 0.09f, 0.16f), skyDay * 0.8f, day);
        }
    }
}
