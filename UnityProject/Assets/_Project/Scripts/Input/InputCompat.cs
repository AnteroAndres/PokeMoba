using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PokeMoba.Input
{
    public enum GameKey { Basic, Skill1, Skill2, Dodge }

    /// <summary>
    /// Teclado para probar en el editor. Funciona con el Input System nuevo o con el antiguo,
    /// según lo que tenga activado el proyecto (Player Settings → Active Input Handling).
    /// </summary>
    public static class InputCompat
    {
        public static Vector2 KeyboardMove()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
#else
            float x = (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                    - (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
            float y = (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                    - (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
#endif
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        public static bool WasPressed(GameKey key)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (key)
            {
                case GameKey.Basic: return k.jKey.wasPressedThisFrame;
                case GameKey.Skill1: return k.kKey.wasPressedThisFrame;
                case GameKey.Skill2: return k.lKey.wasPressedThisFrame;
                case GameKey.Dodge: return k.spaceKey.wasPressedThisFrame;
            }
            return false;
#else
            switch (key)
            {
                case GameKey.Basic: return UnityEngine.Input.GetKeyDown(KeyCode.J);
                case GameKey.Skill1: return UnityEngine.Input.GetKeyDown(KeyCode.K);
                case GameKey.Skill2: return UnityEngine.Input.GetKeyDown(KeyCode.L);
                case GameKey.Dodge: return UnityEngine.Input.GetKeyDown(KeyCode.Space);
            }
            return false;
#endif
        }
    }
}
