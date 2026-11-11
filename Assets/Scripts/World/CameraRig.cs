using UnityEngine;
using UnityEngine.InputSystem;

namespace OpenLife
{
    /// <summary>Orbit camera: WASD pan, Q/E or right-drag rotate, wheel zoom, F focus.</summary>
    public class CameraRig : MonoBehaviour
    {
        public bool InputEnabled = true;

        float yaw = 35f;
        float pitch = 50f;
        float distance = 20f;
        Vector3 focus = new Vector3(-1f, 0f, -1f);

        void Start() { Apply(); }

        public void FocusOn(Vector3 p)
        {
            focus = new Vector3(p.x, 0f, p.z);
        }

        void LateUpdate()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (InputEnabled && kb != null && mouse != null && !UIFactory.IsTyping())
            {
                float dt = Time.unscaledDeltaTime;
                Vector2 move = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;

                if (kb.qKey.isPressed) yaw -= 90f * dt;
                if (kb.eKey.isPressed) yaw += 90f * dt;

                if (mouse.rightButton.isPressed)
                {
                    Vector2 d = mouse.delta.ReadValue();
                    yaw += d.x * 0.2f;
                    pitch = Mathf.Clamp(pitch - d.y * 0.2f, 15f, 85f);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f && !UIFactory.PointerOverUI())
                    distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 1.5f, 4f, 45f);

                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(move.x, 0f, move.y);
                focus += dir * (distance * 0.8f) * dt;
                float lim = WorldBuilder.GridHalf + 12f;
                focus.x = Mathf.Clamp(focus.x, -lim, lim);
                focus.z = Mathf.Clamp(focus.z, -lim, lim);

                if (kb.fKey.wasPressedThisFrame && GameState.Selected != null)
                    FocusOn(GameState.Selected.transform.position);
            }
            Apply();
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            transform.position = focus - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
    }
}
