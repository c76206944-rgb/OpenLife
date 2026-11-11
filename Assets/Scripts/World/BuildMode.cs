using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenLife
{
    [Serializable]
    public class PlacedSave
    {
        public string defId;
        public int x, y, rot;
        public string color;
    }

    /// <summary>
    /// Grid-based build mode. Floors live on one layer, everything else (walls, furniture) on another.
    /// The navmesh is rebuilt shortly after every change so Sims path around new buildings.
    /// </summary>
    public class BuildMode : MonoBehaviour
    {
        public bool Active { get; private set; }
        public ObjectDef Selected { get; private set; }
        public bool DeleteTool { get; private set; }
        public int Rot { get; private set; }
        public string ColorHex = "";

        public event Action StateChanged;

        WorldBuilder world;
        Camera cam;
        readonly Dictionary<Vector2Int, PlacedObject> floorLayer = new Dictionary<Vector2Int, PlacedObject>();
        readonly Dictionary<Vector2Int, PlacedObject> objectLayer = new Dictionary<Vector2Int, PlacedObject>();
        readonly List<PlacedObject> all = new List<PlacedObject>();

        GameObject ghost;
        GameObject gridLines;
        float rebakeTimer = -1f;
        Vector2Int lastPlaced = new Vector2Int(int.MinValue, int.MinValue);

        public void Init(WorldBuilder w, Camera c)
        {
            world = w;
            cam = c;
            BuildGridLines();
            gridLines.SetActive(false);
        }

        // ---------- mode and tool state ----------

        public void SetActive(bool on)
        {
            Active = on;
            if (gridLines != null) gridLines.SetActive(on);
            if (!on) { Selected = null; DeleteTool = false; DestroyGhost(); }
            if (StateChanged != null) StateChanged();
        }

        public void Toggle() { SetActive(!Active); }

        public void Select(ObjectDef d)
        {
            Selected = d;
            DeleteTool = false;
            Rot = 0;
            RebuildGhost();
            if (StateChanged != null) StateChanged();
        }

        public void SelectDelete()
        {
            DeleteTool = true;
            Selected = null;
            DestroyGhost();
            if (StateChanged != null) StateChanged();
        }

        public void SetColor(string hex)
        {
            ColorHex = hex ?? "";
            RebuildGhost();
            if (StateChanged != null) StateChanged();
        }

        // ---------- update ----------

        void Update()
        {
            if (rebakeTimer >= 0f)
            {
                rebakeTimer -= Time.unscaledDeltaTime;
                if (rebakeTimer < 0f && world != null) world.Rebake();
            }

            if (!Active || cam == null) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (kb.rKey.wasPressedThisFrame && Selected != null && !UIFactory.IsTyping())
            {
                Rot = (Rot + 1) % 4;
                RebuildGhost();
            }

            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (Selected != null || DeleteTool) { Selected = null; DeleteTool = false; DestroyGhost(); if (StateChanged != null) StateChanged(); }
                else SetActive(false);
                return;
            }

            bool overUI = UIFactory.PointerOverUI();

            if (DeleteTool)
            {
                if (!overUI && mouse.leftButton.wasPressedThisFrame) TryDelete(mouse.position.ReadValue());
                return;
            }

            if (Selected == null) return;

            if (!CursorCell(mouse.position.ReadValue(), out Vector2Int cell))
            {
                if (ghost != null) ghost.SetActive(false);
                return;
            }

            var f = ObjectFactory.Footprint(Selected, Rot);
            Vector2Int origin = cell - new Vector2Int((f.x - 1) / 2, (f.y - 1) / 2);

            if (ghost == null) RebuildGhost();
            if (ghost != null)
            {
                ghost.SetActive(true);
                ghost.transform.position = ObjectFactory.CellToWorld(Selected, origin, Rot);
                ghost.transform.rotation = Quaternion.Euler(0f, Rot * 90f, 0f);
            }

            bool valid = CanPlace(Selected, origin, Rot);
            bool affordable = GameState.Funds >= Selected.price;
            TintGhost(valid && affordable ? new Color(0.3f, 0.9f, 0.4f) : new Color(0.95f, 0.3f, 0.3f));

            bool paint = Selected.IsFloor || Selected.IsWall;
            bool click = !overUI && (paint
                ? mouse.leftButton.isPressed && (origin != lastPlaced || mouse.leftButton.wasPressedThisFrame)
                : mouse.leftButton.wasPressedThisFrame);

            if (!mouse.leftButton.isPressed) lastPlaced = new Vector2Int(int.MinValue, int.MinValue);

            if (click && valid)
            {
                if (!affordable) { GameState.Notify("Not enough funds."); return; }
                if (Selected.IsFloor && floorLayer.TryGetValue(origin, out var old)) Remove(old, true);
                Place(Selected, origin, Rot, ColorHex);
                GameState.Funds -= Selected.price;
                lastPlaced = origin;
            }
        }

        // ---------- placement ----------

        static bool InBounds(Vector2Int c)
        {
            return c.x >= -WorldBuilder.GridHalf && c.x < WorldBuilder.GridHalf
                && c.y >= -WorldBuilder.GridHalf && c.y < WorldBuilder.GridHalf;
        }

        bool CursorCell(Vector2 screen, out Vector2Int cell)
        {
            cell = default;
            var ray = cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float t)) return false;
            Vector3 p = ray.GetPoint(t);
            cell = new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z));
            return InBounds(cell);
        }

        public bool CanPlace(ObjectDef d, Vector2Int origin, int rot)
        {
            var f = ObjectFactory.Footprint(d, rot);
            for (int x = 0; x < f.x; x++)
                for (int y = 0; y < f.y; y++)
                {
                    var c = origin + new Vector2Int(x, y);
                    if (!InBounds(c)) return false;
                    if (d.IsFloor)
                    {
                        // Don't re-place an identical floor.
                        if (floorLayer.TryGetValue(c, out var ex) && ex.DefId == d.id && ex.ColorHex == (ColorHex ?? "")) return false;
                    }
                    else if (objectLayer.ContainsKey(c)) return false;
                }
            return true;
        }

        public PlacedObject Place(ObjectDef d, Vector2Int origin, int rot, string colorHex)
        {
            var go = ObjectFactory.Create(d, origin, rot, colorHex, false, world.BuildRoot);
            var po = go.GetComponent<PlacedObject>();
            var layer = d.IsFloor ? floorLayer : objectLayer;
            var f = ObjectFactory.Footprint(d, rot);
            for (int x = 0; x < f.x; x++)
                for (int y = 0; y < f.y; y++)
                    layer[origin + new Vector2Int(x, y)] = po;
            all.Add(po);
            MarkDirty();
            return po;
        }

        void Remove(PlacedObject po, bool silent)
        {
            if (po == null) return;
            var layer = po.Def.IsFloor ? floorLayer : objectLayer;
            var f = ObjectFactory.Footprint(po.Def, po.Rot);
            for (int x = 0; x < f.x; x++)
                for (int y = 0; y < f.y; y++)
                {
                    var c = po.Cell + new Vector2Int(x, y);
                    if (layer.TryGetValue(c, out var cur) && cur == po) layer.Remove(c);
                }
            all.Remove(po);
            GameState.Funds += po.Def.price / 2;
            po.gameObject.SetActive(false);
            Destroy(po.gameObject);
            MarkDirty();
        }

        void TryDelete(Vector2 screen)
        {
            var ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 300f)) return;
            var po = hit.collider.GetComponentInParent<PlacedObject>();
            if (po != null) Remove(po, false);
        }

        void MarkDirty() { rebakeTimer = 0.4f; }

        public void ClearAll()
        {
            foreach (var po in all)
            {
                if (po == null) continue;
                po.gameObject.SetActive(false);
                Destroy(po.gameObject);
            }
            all.Clear();
            floorLayer.Clear();
            objectLayer.Clear();
            MarkDirty();
        }

        // ---------- ghost preview ----------

        void RebuildGhost()
        {
            DestroyGhost();
            if (Selected == null || !Active) return;
            ghost = ObjectFactory.Create(Selected, Vector2Int.zero, Rot, ColorHex, true, transform);
            ghost.SetActive(false);
        }

        void DestroyGhost()
        {
            if (ghost != null) Destroy(ghost);
            ghost = null;
        }

        void TintGhost(Color c)
        {
            if (ghost == null) return;
            foreach (var r in ghost.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = MaterialUtil.Shared(c);
        }

        // ---------- grid lines ----------

        void BuildGridLines()
        {
            gridLines = new GameObject("BuildGrid");
            gridLines.transform.SetParent(transform, false);
            var shader = Shader.Find("Sprites/Default");
            var mat = shader != null ? new Material(shader) : MaterialUtil.Make(Color.white);
            int h = WorldBuilder.GridHalf;
            Color col = new Color(1f, 1f, 1f, 0.22f);
            for (int i = -h; i <= h; i++)
            {
                MakeLine(mat, col, new Vector3(i, 0.06f, -h), new Vector3(i, 0.06f, h));
                MakeLine(mat, col, new Vector3(-h, 0.06f, i), new Vector3(h, 0.06f, i));
            }
        }

        void MakeLine(Material mat, Color col, Vector3 a, Vector3 b)
        {
            var go = new GameObject("l");
            go.transform.SetParent(gridLines.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = mat;
            lr.startColor = col;
            lr.endColor = col;
            lr.startWidth = 0.03f;
            lr.endWidth = 0.03f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            go.layer = ObjectFactory.IgnoreRaycastLayer;
        }

        // ---------- save and load ----------

        public List<PlacedSave> Export()
        {
            var list = new List<PlacedSave>();
            foreach (var po in all)
            {
                if (po == null) continue;
                list.Add(new PlacedSave { defId = po.DefId, x = po.Cell.x, y = po.Cell.y, rot = po.Rot, color = po.ColorHex });
            }
            return list;
        }

        public void Import(List<PlacedSave> saved)
        {
            ClearAll();
            if (saved == null) return;
            foreach (var s in saved)
            {
                var d = ContentDatabase.GetObject(s.defId);
                if (d == null) { Debug.LogWarning("[OpenLife] Unknown object in save: " + s.defId); continue; }
                Place(d, new Vector2Int(s.x, s.y), s.rot, s.color);
            }
            if (world != null) world.Rebake();
            rebakeTimer = -1f;
        }

        // ---------- starter lot ----------

        /// <summary>A small furnished home so a new game has something to start with. Free of charge.</summary>
        public void BuildStarterLot()
        {
            ClearAll();
            const int x0 = -4, x1 = 2, y0 = -3, y1 = 1;

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                {
                    TryPlace("floor_wood", x, y, 0);
                    bool edge = x == x0 || x == x1 || y == y0 || y == y1;
                    if (!edge) continue;
                    if (x == -1 && y == y0) TryPlace("doorway", x, y, 0);
                    else TryPlace("wall_basic", x, y, 0);
                }

            TryPlace("bed_single", -3, -2, 0);
            TryPlace("toilet", -2, -2, 0);
            TryPlace("sofa", -1, -2, 0);
            TryPlace("bookshelf", 1, -2, 0);
            TryPlace("shower", -2, 0, 2);
            TryPlace("tv", -1, 0, 2);
            TryPlace("fridge", 1, 0, 2);

            if (world != null) world.Rebake();
            rebakeTimer = -1f;
        }

        void TryPlace(string id, int x, int y, int rot)
        {
            var d = ContentDatabase.GetObject(id);
            if (d == null) return;
            var origin = new Vector2Int(x, y);
            if (!CanPlace(d, origin, rot)) return;
            Place(d, origin, rot, "");
        }
    }
}
