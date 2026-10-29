using System;
using PokeMoba.Input;
using PokeMoba.Modes;
using UnityEngine;

namespace PokeMoba.UI
{
    /// <summary>Esc abre/cierra la pausa de exploración sin interferir con captura o evolución.</summary>
    public class AdventurePauseMenu : MonoBehaviour
    {
        GameContext ctx;
        Func<bool> canPause;
        Action leaveMap;
        Action leaveAdventure;
        bool paused;

        public void Setup(GameContext context, Func<bool> allowed, Action map, Action main)
        {
            ctx = context; canPause = allowed; leaveMap = map; leaveAdventure = main;
        }

        void Update()
        {
            if (ctx == null || !InputCompat.ReleaseMouse()) return;
            if (paused) Resume();
            else Open();
        }

        public void Open()
        {
            if (paused || ctx.Menus.IsOpen || Time.timeScale <= 0f || !canPause()) return;
            paused = true;
            Time.timeScale = 0f;
            var panel = ctx.Menus.Open("Aventura en pausa", "Esc para continuar · Tu progreso se guarda al salir", .8f);
            var anchor = new Vector2(.5f, .5f);
            var size = new Vector2(600f, 100f);
            MenuScreens.Button(panel, "Continuar", new Color(.2f, .55f, .35f), anchor, new Vector2(0f, 130f), size, Resume, 36);
            MenuScreens.Button(panel, "Volver al mapa de Kanto", new Color(.25f, .4f, .6f), anchor, Vector2.zero, size,
                () => Exit(leaveMap), 32);
            MenuScreens.Button(panel, "Salir de Aventura", new Color(.6f, .25f, .25f), anchor, new Vector2(0f, -130f), size,
                () => Exit(leaveAdventure), 34);
        }

        void Resume()
        {
            paused = false;
            ctx.Menus.Close();
            Time.timeScale = 1f;
            ctx.Follow.CapturePointer();
        }

        void Exit(Action destination)
        {
            paused = false;
            Time.timeScale = 1f;
            ctx.Menus.Close();
            destination();
        }

        void OnDestroy()
        {
            if (paused) Time.timeScale = 1f;
        }
    }
}
