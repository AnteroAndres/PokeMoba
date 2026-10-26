using System;
using System.Collections.Generic;
using PokeMoba.Combat;
using PokeMoba.Game;
using PokeMoba.Prototype;
using PokeMoba.UI;
using UnityEngine;

namespace PokeMoba.Modes
{
    /// <summary>Todo lo que comparten los modos: cámara, interfaz, menús, partida guardada y el "mundo" actual.</summary>
    public class GameContext
    {
        public MonoBehaviour Runner;
        public Camera Camera;
        public FollowCamera Follow;
        public Canvas Canvas;
        public CombatHUD Hud;
        public MenuScreens Menus;
        public Action ReturnToMainMenu;

        public SaveData Save => SaveService.Current;

        Transform world;

        /// <summary>Borra el escenario anterior (y todo lo que haya en combate) y crea uno vacío.</summary>
        public Transform NewWorld(string name)
        {
            ClearWorld();
            world = new GameObject(name).transform;
            return world;
        }

        public void ClearWorld()
        {
            Time.timeScale = 1f;
            Runner.StopAllCoroutines();
            Hud.ClearExtras();
            Hud.gameObject.SetActive(false);
            Menus.Close();

            if (world != null) UnityEngine.Object.Destroy(world.gameObject);
            world = null;

            foreach (var c in new List<Combatant>(Combatant.All)) UnityEngine.Object.Destroy(c.gameObject);
            foreach (var p in new List<Projectile>(Projectile.Active)) UnityEngine.Object.Destroy(p.gameObject);
            foreach (var a in new List<AreaTelegraph>(AreaTelegraph.Active)) UnityEngine.Object.Destroy(a.gameObject);
            Follow.SetThirdPerson(false);
            Follow.SetTargets(null, null);
        }
    }

    /// <summary>Un modo de juego (Práctica, Duelo, Aventura, MOBA, Pokédex).</summary>
    public abstract class GameMode
    {
        protected readonly GameContext Ctx;

        protected GameMode(GameContext ctx) => Ctx = ctx;

        public abstract void Enter();

        protected SaveData Save => Ctx.Save;
    }
}
