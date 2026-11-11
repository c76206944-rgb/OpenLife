using System.Collections.Generic;
using UnityEngine;

namespace OpenLife
{
    /// <summary>
    /// Makes simple colored materials that work in URP or the built-in pipeline.
    /// Note: Shader.Find only works in builds if the shader is in Project Settings,
    /// Graphics, Always Included Shaders (see README).
    /// </summary>
    public static class MaterialUtil
    {
        static Shader shader;
        static readonly Dictionary<string, Material> shared = new Dictionary<string, Material>();

        static Shader GetShader()
        {
            if (shader != null) return shader;
            shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            return shader;
        }

        /// <summary>A new material the caller owns and may change.</summary>
        public static Material Make(Color c)
        {
            var m = new Material(GetShader());
            m.color = c;
            return m;
        }

        /// <summary>A cached material for objects that never change color.</summary>
        public static Material Shared(Color c)
        {
            string key = ColorUtility.ToHtmlStringRGB(c);
            if (!shared.TryGetValue(key, out var m) || m == null)
            {
                m = Make(c);
                shared[key] = m;
            }
            return m;
        }

        public static void Tint(GameObject go, Color c)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.material.color = c;
        }

        public static void RemoveColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>())
                Object.Destroy(c);
        }
    }
}
