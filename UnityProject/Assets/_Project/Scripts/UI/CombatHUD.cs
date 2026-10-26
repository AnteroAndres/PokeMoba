using System;
using System.Collections.Generic;
using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Input;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>
    /// Interfaz de combate común a todos los modos: barras de vida y estamina, joystick, botones de
    /// habilidad y esquiva, texto de estado, avisos y botones extra que añade cada modo.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }
        public AbilityButton[] AbilityButtons { get; private set; }
        public AbilityButton DodgeButton { get; private set; }

        class Panel
        {
            public GameObject Root;
            public Text Name;
            public Text Status;
            public Text HpText;
            public RectTransform HpFill;
            public RectTransform StaminaFill;
        }

        class SkillView
        {
            public Image Background;
            public Image Cooldown;
            public Text Label;
            public Text CooldownText;
        }

        RectTransform root;
        Panel playerPanel;
        Panel enemyPanel;
        SkillView[] skills;
        Image dodgeBackground;
        Text statusText;
        Text toastText;
        float toastTimer;
        readonly List<GameObject> extras = new List<GameObject>();

        readonly List<Button> keyboardActions = new List<Button>();

        Combatant player;
        Combatant enemy;
        AbilityCaster playerCaster;
        Combatant trainer;
        TrainerPanel trainerPanel;

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color BarBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        public static CombatHUD Create(Transform canvas, bool showKeyboardHint)
        {
            var rect = UIFactory.CreateRect("HUD", canvas).Stretch();
            var hud = rect.gameObject.AddComponent<CombatHUD>();
            hud.Build(rect, showKeyboardHint);
            return hud;
        }

        /// <summary>Conecta el HUD con el Pokémon del jugador; <paramref name="enemy"/> puede ser null.</summary>
        public void Bind(Combatant player, Combatant enemy)
        {
            this.player = player;
            playerCaster = player.GetComponent<AbilityCaster>();
            RefreshPlayer();
            SetEnemy(enemy);
        }

        /// <summary>Vuelve a leer nombre y movimientos (tras evolucionar o subir de nivel).</summary>
        public void RefreshPlayer()
        {
            if (player == null) return;
            playerPanel.Name.text = Title(player);
            for (int i = 0; i < skills.Length; i++)
            {
                var move = playerCaster.GetMove(i);
                skills[i].Background.color = move.Type.TypeColor();
                skills[i].Label.text = move.Name + (Application.isMobilePlatform ? "" : " [" + new[] { "J", "K", "L", "I" }[i] + "]");
            }
        }

        public void SetEnemy(Combatant newEnemy)
        {
            enemy = newEnemy;
            enemyPanel.Root.SetActive(enemy != null);
            if (enemy != null) enemyPanel.Name.text = Title(enemy);
        }

        /// <summary>Barra de vida del entrenador (Aventura); null la oculta.</summary>
        public void SetTrainer(Combatant newTrainer)
        {
            trainer = newTrainer;
            trainerPanel.Root.SetActive(trainer != null);
            if (trainer != null) trainerPanel.Name.text = trainer.DisplayName;
        }

        class TrainerPanel
        {
            public GameObject Root;
            public Text Name;
            public RectTransform HpFill;
        }

        static TrainerPanel BuildTrainerPanel(RectTransform rect)
        {
            var bg = UIFactory.CreateImage("Entrenador", rect, PanelColor);
            bg.raycastTarget = false;
            var panelRect = bg.rectTransform.Place(new Vector2(0f, 1f), new Vector2(30f, -178f), new Vector2(400f, 52f));
            var panel = new TrainerPanel { Root = bg.gameObject };

            panel.Name = UIFactory.CreateText("Nombre", panelRect, "", 22, Color.white, TextAnchor.MiddleLeft);
            panel.Name.rectTransform.Place(new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(150f, 40f));

            var bar = UIFactory.CreateBar("Vida", panelRect, BarBackground, new Color(0.95f, 0.75f, 0.3f));
            panel.HpFill = bar;
            ((RectTransform)bar.parent).Place(new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(220f, 18f));

            bg.gameObject.SetActive(false);
            return panel;
        }

        void UpdateTrainerPanel()
        {
            UIFactory.SetBarFill(trainerPanel.HpFill, trainer.MaxHp > 0f ? trainer.Hp / trainer.MaxHp : 0f);
        }

        public void SetStatus(string text) => statusText.text = text ?? "";

        /// <summary>Aviso grande en el centro de la pantalla durante unos segundos.</summary>
        public void Toast(string text, float seconds = 2.5f)
        {
            toastText.text = text;
            toastTimer = seconds;
        }

        /// <summary>Botón extra del modo (se borra con ClearExtras). Devuelve el botón para cambiar su texto.</summary>
        public Button AddButton(string label, Color color, Vector2 anchor, Vector2 position, Vector2 size, Action onClick, int fontSize = 24)
        {
            var button = UIFactory.CreateButton(label, root, label, fontSize, color, onClick);
            ((RectTransform)button.transform).Place(anchor, position, size);
            if (!Application.isMobilePlatform && keyboardActions.Count < 12)
            {
                int index = keyboardActions.Count;
                keyboardActions.Add(button);
                var keyHint = UIFactory.CreateText("Atajo", button.transform, "F" + (index + 1), 18,
                    Color.white, TextAnchor.UpperRight);
                keyHint.rectTransform.Stretch();
                keyHint.raycastTarget = false;
            }
            extras.Add(button.gameObject);
            return button;
        }

        public void ClearExtras()
        {
            foreach (var go in extras) if (go != null) Destroy(go);
            extras.Clear();
            keyboardActions.Clear();
            SetTrainer(null);
            SetStatus("");
            toastTimer = 0f;
            toastText.text = "";
        }

        static string Title(Combatant c)
        {
            string types = string.Join(" / ", Array.ConvertAll(c.Species.Types, t => t.DisplayName()));
            string shiny = c.Shiny ? " ★" : "";
            return $"{c.DisplayName}{shiny}  Nv. {c.Stats.Level}   <size=22>{types}</size>";
        }

        void Update()
        {
            // Invocar solo una acción: puede reconstruir el mundo y borrar esta lista.
            for (int i = 0; i < keyboardActions.Count; i++)
            {
                var button = keyboardActions[i];
                if (InputCompat.ExtraPressed(i) && button != null && button.IsActive() && button.interactable)
                {
                    button.onClick.Invoke();
                    break;
                }
            }

            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                var c = toastText.color;
                c.a = Mathf.Clamp01(toastTimer * 2f);
                toastText.color = c;
            }

            if (player == null) return;

            UpdatePanel(playerPanel, player);
            if (enemy != null) UpdatePanel(enemyPanel, enemy);
            if (trainer != null) UpdateTrainerPanel();

            var motor = player.Motor;
            UIFactory.SetBarFill(playerPanel.StaminaFill, motor.Stamina / CombatTuning.MaxStamina);
            dodgeBackground.color = motor.Stamina >= CombatTuning.DodgeCost
                ? new Color(0.35f, 0.75f, 1f)
                : new Color(0.25f, 0.3f, 0.35f);

            for (int i = 0; i < skills.Length; i++)
            {
                skills[i].Cooldown.fillAmount = playerCaster.CooldownFraction(i);
                float remaining = playerCaster.CooldownRemaining(i);
                skills[i].CooldownText.text = remaining > 0.95f ? Mathf.CeilToInt(remaining).ToString() : "";
            }
        }

        static void UpdatePanel(Panel panel, Combatant c)
        {
            float fraction = c.MaxHp > 0f ? c.Hp / c.MaxHp : 0f;
            UIFactory.SetBarFill(panel.HpFill, fraction);
            panel.HpFill.GetComponent<Image>().color =
                fraction > 0.5f ? new Color(0.3f, 0.85f, 0.35f) :
                fraction > 0.2f ? new Color(0.95f, 0.8f, 0.2f) : new Color(0.9f, 0.25f, 0.2f);
            panel.HpText.text = $"{Mathf.CeilToInt(c.Hp)} / {Mathf.CeilToInt(c.MaxHp)}";
            panel.Status.text = c.Status == StatusEffect.None ? "" : c.Status.ToString().ToUpperInvariant();
        }

        // --- Construcción ---

        void Build(RectTransform rect, bool showKeyboardHint)
        {
            root = rect;
            playerPanel = BuildPanel(rect, new Vector2(0f, 1f), new Vector2(30f, -30f), withStamina: true);
            enemyPanel = BuildPanel(rect, new Vector2(1f, 1f), new Vector2(-30f, -30f), withStamina: false);

            statusText = UIFactory.CreateText("Estado", rect, "", 30, Color.white, TextAnchor.UpperCenter);
            statusText.supportRichText = true;
            statusText.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(700f, 110f));

            toastText = UIFactory.CreateText("Aviso", rect, "", 56, new Color(1f, 0.95f, 0.6f), TextAnchor.MiddleCenter);
            toastText.supportRichText = true;
            toastText.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1400f, 200f));

            if (Application.isMobilePlatform)
            {
                var lookZone = UIFactory.CreateImage("Arrastrar para mirar", rect, Color.clear);
                lookZone.rectTransform.anchorMin = new Vector2(0.5f, 0.38f);
                lookZone.rectTransform.anchorMax = new Vector2(1f, 0.8f);
                lookZone.rectTransform.offsetMin = Vector2.zero;
                lookZone.rectTransform.offsetMax = Vector2.zero;
                lookZone.gameObject.AddComponent<CameraLookPad>();
            }
            BuildJoystick(rect);

            var bottomRight = new Vector2(1f, 0f);
            AbilityButtons = new AbilityButton[AbilityCaster.SlotCount];
            skills = new SkillView[AbilityCaster.SlotCount];
            Vector2[] positions = { new Vector2(-110f, 90f), new Vector2(-370f, 40f), new Vector2(-380f, 230f), new Vector2(-200f, 340f) };
            float[] sizes = { 220f, 140f, 140f, 140f };
            for (int i = 0; i < AbilityCaster.SlotCount; i++)
                AbilityButtons[i] = BuildSkillButton(rect, i, bottomRight, positions[i], sizes[i]);

            DodgeButton = BuildDodgeButton(rect, bottomRight, new Vector2(-40f, 340f), 140f);
            trainerPanel = BuildTrainerPanel(rect);

            if (showKeyboardHint)
            {
                var hint = UIFactory.CreateText("Ayuda teclado", rect,
                    "WASD / flechas mover · J K L I movimientos · Espacio esquivar · F objetivo · Q/E cámara · F1–F12 acciones",
                    22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerCenter);
                hint.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(1400f, 40f));
            }
        }

        static Panel BuildPanel(RectTransform rect, Vector2 anchor, Vector2 position, bool withStamina)
        {
            var bg = UIFactory.CreateImage("Panel", rect, PanelColor);
            bg.raycastTarget = false;
            var panelRect = bg.rectTransform.Place(anchor, position, new Vector2(560f, withStamina ? 140f : 110f));

            var panel = new Panel { Root = bg.gameObject };
            var align = anchor.x < 0.5f ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

            panel.Name = UIFactory.CreateText("Nombre", panelRect, "", 32, Color.white, align);
            panel.Name.supportRichText = true;
            panel.Name.rectTransform.Place(new Vector2(anchor.x, 1f), new Vector2(anchor.x < 0.5f ? 16f : -16f, -10f), new Vector2(520f, 40f));

            panel.Status = UIFactory.CreateText("Estado", panelRect, "", 24, new Color(1f, 0.55f, 0.3f),
                anchor.x < 0.5f ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            panel.Status.rectTransform.Place(new Vector2(anchor.x, 1f), new Vector2(anchor.x < 0.5f ? 16f : -16f, -14f), new Vector2(520f, 34f));

            var hpBar = UIFactory.CreateBar("Vida", panelRect, BarBackground, Color.green);
            panel.HpFill = hpBar;
            ((RectTransform)hpBar.parent).Place(new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(528f, 32f));
            panel.HpText = UIFactory.CreateText("PS", hpBar.parent, "", 22, Color.white, TextAnchor.MiddleCenter);
            panel.HpText.rectTransform.Stretch();

            if (withStamina)
            {
                var stamina = UIFactory.CreateBar("Estamina", panelRect, BarBackground, new Color(0.35f, 0.75f, 1f));
                panel.StaminaFill = stamina;
                ((RectTransform)stamina.parent).Place(new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(528f, 16f));
            }

            return panel;
        }

        void BuildJoystick(RectTransform rect)
        {
            // Zona invisible que recibe el toque: mitad izquierda, debajo de los paneles.
            var zone = UIFactory.CreateImage("Zona joystick", rect, new Color(0f, 0f, 0f, 0f));
            zone.rectTransform.anchorMin = Vector2.zero;
            zone.rectTransform.anchorMax = new Vector2(0.45f, 0.75f);
            zone.rectTransform.offsetMin = Vector2.zero;
            zone.rectTransform.offsetMax = Vector2.zero;

            const float radius = 120f;
            var background = UIFactory.CreateImage("Fondo", zone.transform, new Color(1f, 1f, 1f, 0.18f), UIFactory.Circle);
            background.raycastTarget = false;
            background.rectTransform.Place(new Vector2(0f, 0f), new Vector2(120f, 100f), Vector2.one * radius * 2f);
            background.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            background.rectTransform.anchoredPosition = new Vector2(260f, 240f);

            var knob = UIFactory.CreateImage("Palanca", background.transform, new Color(1f, 1f, 1f, 0.6f), UIFactory.Circle);
            knob.raycastTarget = false;
            knob.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * 110f);

            Joystick = zone.gameObject.AddComponent<VirtualJoystick>();
            Joystick.Setup(background.rectTransform, knob.rectTransform, radius);
        }

        AbilityButton BuildSkillButton(RectTransform rect, int slot, Vector2 anchor, Vector2 position, float size)
        {
            var bg = UIFactory.CreateImage($"Habilidad {slot}", rect, Color.gray, UIFactory.Circle);
            bg.alphaHitTestMinimumThreshold = 0.1f;
            bg.rectTransform.Place(anchor, position, Vector2.one * size);

            var cooldown = UIFactory.CreateImage("Recarga", bg.transform, new Color(0f, 0f, 0f, 0.6f), UIFactory.Circle);
            cooldown.raycastTarget = false;
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;
            cooldown.rectTransform.Stretch();

            var label = UIFactory.CreateText("Nombre", bg.transform, "", slot == 0 ? 28 : 24, Color.white, TextAnchor.LowerCenter);
            label.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, -36f), new Vector2(size + 80f, 34f));

            var cooldownText = UIFactory.CreateText("Segundos", bg.transform, "", 56, Color.white, TextAnchor.MiddleCenter);
            cooldownText.rectTransform.Stretch();

            skills[slot] = new SkillView { Background = bg, Cooldown = cooldown, Label = label, CooldownText = cooldownText };
            return bg.gameObject.AddComponent<AbilityButton>();
        }

        AbilityButton BuildDodgeButton(RectTransform rect, Vector2 anchor, Vector2 position, float size)
        {
            dodgeBackground = UIFactory.CreateImage("Esquiva", rect, new Color(0.35f, 0.75f, 1f), UIFactory.Circle);
            dodgeBackground.alphaHitTestMinimumThreshold = 0.1f;
            dodgeBackground.rectTransform.Place(anchor, position, Vector2.one * size);

            var label = UIFactory.CreateText("Texto", dodgeBackground.transform, Application.isMobilePlatform ? "Esquivar" : "Espacio", 24, Color.white, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch();

            return dodgeBackground.gameObject.AddComponent<AbilityButton>();
        }
    }
}
