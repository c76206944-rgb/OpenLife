using UnityEngine;

namespace OpenLife
{
    /// <summary>Data attached to every object the player has placed.</summary>
    public class PlacedObject : MonoBehaviour
    {
        public string DefId;
        public string ColorHex;
        public Vector2Int Cell;
        public int Rot;
        public ObjectDef Def;
        public Renderer MeshRenderer;
    }

    /// <summary>Builds placeable objects from an ObjectDef (primitive shapes or a Resources prefab).</summary>
    public static class ObjectFactory
    {
        public const int IgnoreRaycastLayer = 2;

        public static Vector2Int Footprint(ObjectDef d, int rot)
        {
            return (rot % 2 == 0) ? new Vector2Int(d.sizeX, d.sizeZ) : new Vector2Int(d.sizeZ, d.sizeX);
        }

        public static Vector3 CellToWorld(ObjectDef d, Vector2Int cell, int rot)
        {
            var f = Footprint(d, rot);
            return new Vector3(cell.x + f.x * 0.5f, 0f, cell.y + f.y * 0.5f);
        }

        public static GameObject Create(ObjectDef d, Vector2Int cell, int rot, string colorHex, bool ghost, Transform parent)
        {
            var root = new GameObject(d.name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = CellToWorld(d, cell, rot);
            root.transform.rotation = Quaternion.Euler(0f, rot * 90f, 0f);

            var po = root.AddComponent<PlacedObject>();
            po.DefId = d.id;
            po.Def = d;
            po.Cell = cell;
            po.Rot = ((rot % 4) + 4) % 4;
            po.ColorHex = colorHex ?? "";

            Color color = Util.ParseColor(string.IsNullOrEmpty(colorHex) ? d.color : colorHex, Color.gray);
            bool built = false;

            if (!string.IsNullOrEmpty(d.prefab))
            {
                var prefab = Resources.Load<GameObject>(d.prefab);
                if (prefab != null)
                {
                    var inst = Object.Instantiate(prefab, root.transform);
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    po.MeshRenderer = inst.GetComponentInChildren<Renderer>();
                    built = true;
                }
                else Debug.LogWarning("[OpenLife] Prefab not found in Resources: " + d.prefab);
            }

            if (!built) BuildPrimitive(root.transform, d, color, po);

            if (ghost)
            {
                MaterialUtil.RemoveColliders(root);
                SetLayer(root, IgnoreRaycastLayer);
                return root;
            }

            if (root.GetComponentInChildren<Collider>() == null)
            {
                var bc = root.AddComponent<BoxCollider>();
                bc.size = new Vector3(d.sizeX, d.height, d.sizeZ);
                bc.center = new Vector3(0f, d.height * 0.5f, 0f);
            }

            // Walls and furniture are marked Not Walkable by WorldBuilder.Rebake (via NavMeshBuildMarkup).

            if (d.interactions != null && d.interactions.Length > 0)
            {
                var it = root.AddComponent<Interactable>();
                it.Def = d;
            }

            return root;
        }

        static void BuildPrimitive(Transform root, ObjectDef d, Color color, PlacedObject po)
        {
            bool tile = d.IsFloor || d.IsWall;
            float sx = tile ? d.sizeX : d.sizeX * 0.92f;
            float sz = tile ? d.sizeZ : d.sizeZ * 0.92f;
            float h = Mathf.Max(0.02f, d.height);
            float m = Mathf.Min(sx, sz);

            PrimitiveType type = PrimitiveType.Cube;
            Vector3 scale = new Vector3(sx, h, sz);
            float y = h * 0.5f;

            switch (d.shape)
            {
                case "cylinder":
                    type = PrimitiveType.Cylinder;
                    scale = new Vector3(m * 0.8f, h * 0.5f, m * 0.8f);
                    break;
                case "sphere":
                    type = PrimitiveType.Sphere;
                    scale = new Vector3(h, h, h);
                    break;
                case "capsule":
                    type = PrimitiveType.Capsule;
                    scale = new Vector3(m * 0.6f, h * 0.5f, m * 0.6f);
                    break;
                case "lintel":
                    scale = new Vector3(sx, 0.6f, sz);
                    y = h - 0.3f;
                    break;
            }

            var mesh = GameObject.CreatePrimitive(type);
            mesh.name = "Mesh";
            mesh.transform.SetParent(root, false);
            mesh.transform.localPosition = new Vector3(0f, y, 0f);
            mesh.transform.localScale = scale;
            var r = mesh.GetComponent<Renderer>();
            r.sharedMaterial = MaterialUtil.Shared(color);
            po.MeshRenderer = r;

            // Small dark marker on the front (+Z) face so players can see which way it faces.
            if (!tile && d.interactions != null && d.interactions.Length > 0)
            {
                var front = GameObject.CreatePrimitive(PrimitiveType.Cube);
                front.name = "FrontMarker";
                Object.Destroy(front.GetComponent<Collider>());
                front.transform.SetParent(root, false);
                front.transform.localPosition = new Vector3(0f, Mathf.Min(h * 0.5f, 0.6f), d.sizeZ * 0.5f);
                front.transform.localScale = new Vector3(sx * 0.5f, Mathf.Min(h * 0.2f, 0.12f), 0.06f);
                front.GetComponent<Renderer>().sharedMaterial = MaterialUtil.Shared(color * 0.45f);
            }
        }

        public static void Recolor(PlacedObject po, string hex)
        {
            po.ColorHex = hex ?? "";
            if (po.MeshRenderer == null || po.Def == null) return;
            Color c = Util.ParseColor(string.IsNullOrEmpty(hex) ? po.Def.color : hex, Color.gray);
            po.MeshRenderer.sharedMaterial = MaterialUtil.Shared(c);
        }

        public static void SetLayer(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }
    }
}
