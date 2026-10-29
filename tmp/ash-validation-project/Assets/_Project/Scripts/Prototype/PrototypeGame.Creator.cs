#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PokeMoba.Combat;
using PokeMoba.Data.Pokedex;
using PokeMoba.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.Prototype
{
    public partial class PrototypeGame
    {
        [SerializeField] CreatorOptions creator = new CreatorOptions();
        public CreatorOptions Creator => creator;
        public string CreatorStatus { get; private set; } = "Solo Editor. Los recursos y niveles se guardan en tu partida.";
        readonly HashSet<Combatant> creatorActors = new HashSet<Combatant>();
        bool creatorOpen, creatorVitals, creatorBackup;
        float creatorPreviousTime;
        CursorLockMode creatorCursor;
        bool creatorCursorVisible;
        GameObject creatorBlocker;
        Vector2 creatorScroll;

        void OnValidate() { if (creator == null) creator = new CreatorOptions(); creator.SyncItems(); }
        void Start() { creator.SyncItems(); if (creator.ApplyOnStart) CreatorResources(); }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f10Key.wasPressedThisFrame) ToggleCreator();
#else
            if (UnityEngine.Input.GetKeyDown(KeyCode.F10)) ToggleCreator();
#endif
            if (creatorVitals)
                foreach (var actor in Combatant.All)
                    if (IsCreatorActor(actor) && creatorActors.Add(actor)) ApplyCreatorVitals(actor);
        }
        static bool IsCreatorActor(Combatant actor) => actor != null && actor.Species != null && actor.Team == 0 && !actor.IsStructure;
        void ApplyCreatorVitals(Combatant actor) => actor.SetCreatorVitals(actor.Species.DexNumber == 0 ? creator.TrainerHealth : creator.PokemonHealth, creator.HealthPercent / 100f, creator.Invulnerable);
        public void ToggleCreator()
        {
            if (!Application.isPlaying || ctx == null) return;
            creatorOpen = !creatorOpen;
            if (creatorOpen)
            {
                creatorPreviousTime = Time.timeScale; Time.timeScale = 0;
                creatorCursor = Cursor.lockState; creatorCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                creatorBlocker = new GameObject("Panel privado del creador", typeof(RectTransform), typeof(Image));
                creatorBlocker.transform.SetParent(ctx.Canvas.transform, false);
                var rect = (RectTransform)creatorBlocker.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                creatorBlocker.GetComponent<Image>().color = new Color(0,0,0,.7f);
            }
            else CloseCreator();
        }
        void CloseCreator()
        {
            creatorOpen = false; Time.timeScale = creatorPreviousTime;
            Cursor.lockState = creatorCursor; Cursor.visible = creatorCursorVisible;
            if (creatorBlocker != null) Destroy(creatorBlocker);
        }
        void OnDisable() { if (creatorOpen) CloseCreator(); }
        void CreatorAction(Action action, string message, bool save = true)
        {
            if (!Application.isPlaying || ctx == null) return;
            try
            {
                if (save && !creatorBackup)
                {
                    var path = Path.Combine(Application.persistentDataPath,"pokemoba_save.json");
                    if (File.Exists(path)) File.Copy(path,path + ".creator-backup-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff"));
                    creatorBackup = true;
                }
                action(); if (save) SaveService.Save();
                ctx.Hud.RefreshPlayer(); CreatorStatus = message;
            }
            catch (Exception error) { CreatorStatus = error.Message; Debug.LogWarning("Creador: " + error.Message); }
        }
        public void CreatorResources() => CreatorAction(() => creator.ApplyResources(ctx.Save), "Dinero, objetos y desbloqueos guardados. Vuelve al lobby para actualizar sus botones.");
        public void CreatorLevel() => CreatorAction(() =>
        {
            var pokemon = creator.ApplyLevel(ctx.Save);
            foreach (var actor in Combatant.All)
                if (actor != null && actor.OwnedInstance == pokemon)
                {
                    var species = PokemonDatabase.Get(pokemon.Key);
                    actor.Reinitialize(species,pokemon.Level,PokemonDatabase.MovesAt(species,pokemon.Level),pokemon.HpFraction);
                    if (creatorVitals) ApplyCreatorVitals(actor);
                }
        }, "Nivel y experiencia actualizados y guardados.");
        public void CreatorGive() => CreatorAction(() => creator.GivePokemon(ctx.Save), "Pokémon añadido al equipo; si está lleno, al PC.");
        public void CreatorHealth() => CreatorAction(() =>
        {
            creatorVitals = true; creatorActors.Clear();
            foreach (var actor in Combatant.All) if (IsCreatorActor(actor)) { creatorActors.Add(actor); ApplyCreatorVitals(actor); }
        }, "Vida aplicada a tus combatientes. También se aplicará cuando aparezcan. Dura hasta salir de Play.", false);
        public void CreatorHeal() => CreatorAction(() =>
        {
            ctx.Save.HealAll();
            foreach (var actor in Combatant.All) if (IsCreatorActor(actor)) actor.Revive();
        }, "Equipo y entrenador curados.");
        static int CreatorNumber(string label, int value, int min, int max)
        {
            GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(260));
            var raw = GUILayout.TextField(value.ToString(), GUILayout.Width(115));
            if (int.TryParse(raw,out int parsed)) value = Mathf.Clamp(parsed,min,max);
            GUILayout.EndHorizontal(); return value;
        }
        void OnGUI()
        {
            if (!creatorOpen) return;
            float width = Mathf.Min(620,Screen.width-20), height = Mathf.Min(700,Screen.height-20);
            GUILayout.BeginArea(new Rect((Screen.width-width)/2,10,width,height),"CREADOR · Solo tu Editor · F10 para cerrar",GUI.skin.window);
            creatorScroll = GUILayout.BeginScrollView(creatorScroll);
            GUILayout.Label("La partida está pausada. Los cambios se aplican con los botones.");
            creator.Money = CreatorNumber("Dinero",creator.Money,0,1000000000);
            creator.UnlockAll = GUILayout.Toggle(creator.UnlockAll,"Desbloquear zonas y modos");
            creator.SyncItems();
            foreach (var item in creator.Inventory) if (item != null) item.Amount = CreatorNumber(Items.Name(item.Key),item.Amount,0,1000000000);
            if (GUILayout.Button("Guardar dinero y objetos")) CreatorResources();
            GUILayout.Space(12);
            creator.TeamSlot = CreatorNumber("Puesto del equipo",creator.TeamSlot,1,6);
            int slot = creator.TeamSlot-1;
            GUILayout.Label(slot < ctx.Save.Team.Count ? "Seleccionado: " + ctx.Save.Team[slot].Key : "Puesto vacío");
            creator.PokemonLevel = CreatorNumber("Nivel",creator.PokemonLevel,1,100);
            if (GUILayout.Button("Aplicar nivel al Pokémon seleccionado")) CreatorLevel();
            creator.PokemonDex = CreatorNumber("Nuevo Pokémon: Pokédex",creator.PokemonDex,1,151);
            var species = PokemonDatabase.Get(creator.PokemonDex);
            GUILayout.Label(species != null ? species.Name : "Especie no disponible");
            creator.Shiny = GUILayout.Toggle(creator.Shiny,"Shiny");
            if (GUILayout.Button("Añadir este Pokémon al equipo / PC")) CreatorGive();
            GUILayout.Space(12);
            creator.TrainerHealth = CreatorNumber("Vida máxima: entrenador",creator.TrainerHealth,1,1000000000);
            creator.PokemonHealth = CreatorNumber("Vida máxima: Pokémon",creator.PokemonHealth,1,1000000000);
            creator.HealthPercent = CreatorNumber("Vida actual (%)",creator.HealthPercent,1,100);
            creator.Invulnerable = GUILayout.Toggle(creator.Invulnerable,"Invulnerabilidad de mis combatientes");
            if (GUILayout.Button("Aplicar vida durante esta prueba")) CreatorHealth();
            if (GUILayout.Button("Curar a todo mi equipo")) CreatorHeal();
            GUILayout.Label(CreatorStatus);
            if (GUILayout.Button("Cerrar y continuar")) ToggleCreator();
            GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
#endif
