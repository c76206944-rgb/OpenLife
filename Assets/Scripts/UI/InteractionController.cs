using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenLife
{
    /// <summary>Handles clicking Sims and objects in the world (outside build mode).</summary>
    public class InteractionController : MonoBehaviour
    {
        public bool Enabled = true;

        Camera cam;
        BuildMode build;
        InteractionMenu menu;

        public void Init(Camera c, BuildMode b, InteractionMenu m)
        {
            cam = c;
            build = b;
            menu = m;
        }

        void Update()
        {
            if (!Enabled || cam == null || (build != null && build.Active)) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (UIFactory.PointerOverUI()) return;

            menu.Hide();

            Vector2 screen = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(screen);
            if (!Physics.Raycast(ray, out var hit, 300f)) return;

            var sim = hit.collider.GetComponentInParent<SimCharacter>();
            if (sim != null) { OnSimClicked(sim, screen); return; }

            var it = hit.collider.GetComponentInParent<Interactable>();
            if (it != null) OnObjectClicked(it, screen);
        }

        void OnSimClicked(SimCharacter sim, Vector2 screen)
        {
            var current = GameState.Selected;
            if (current == null || current == sim)
            {
                GameState.Select(sim);
                return;
            }

            var items = new List<KeyValuePair<string, Action>>
            {
                new KeyValuePair<string, Action>("Chat with " + sim.Data.name, () => current.EnqueueChat(sim)),
                new KeyValuePair<string, Action>("Switch to " + sim.Data.name, () => GameState.Select(sim))
            };
            menu.Show(sim.Data.name, items, screen);
        }

        void OnObjectClicked(Interactable it, Vector2 screen)
        {
            var sim = GameState.Selected;
            if (sim == null || it.Def == null || it.Def.interactions == null || it.Def.interactions.Length == 0) return;

            var items = new List<KeyValuePair<string, Action>>();
            foreach (var def in it.Def.interactions)
            {
                var d = def;
                string label = d.name + (d.money > 0 ? "  (+$" + d.money + ")" : "");
                items.Add(new KeyValuePair<string, Action>(label, () => sim.Enqueue(it, d)));
            }
            menu.Show(it.Def.name, items, screen);
        }
    }
}
