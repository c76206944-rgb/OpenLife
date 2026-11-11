using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace OpenLife
{
    /// <summary>An object Sims can use. Its Def lists the available interactions.</summary>
    public class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public ObjectDef Def;
        public SimCharacter User;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>A walkable spot in front of the object (its local +Z side).</summary>
        public Vector3 UsePoint()
        {
            float depth = Def != null ? Def.sizeZ * 0.5f : 0.5f;
            Vector3 p = transform.position + transform.forward * (depth + 0.7f);
            p.y = 0f;
            if (NavMesh.SamplePosition(p, out var hit, 2f, NavMesh.AllAreas)) return hit.position;
            return p;
        }
    }
}
