using PokeMoba.Combat;
using PokeMoba.Data;
using PokeMoba.Input;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Interfaz de combate: barras de vida y estamina, joystick, botones de habilidad y esquiva.</summary>
    public class CombatHUD : MonoBehaviour
    {
        public VirtualJoystick Joystick { get; private set; }
        public AbilityButton[] AbilityButtons { get; private set; }
        public AbilityButton DodgeButton { get; private set; }

        class Panel
        {
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

        Panel playerPanel;
        Panel enemyPanel;
        SkillView[] skills;
        Image dodgeBackground;

        Combatant player;
        Combatant enemy;
        AbilityCaster playerCaster;

        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.45f);
        static readonly Color BarBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        public static CombatHUD Create(Transform canvas, bool showKeyboardHint)
        {
            var root = UIFactory.CreateRect("HUD", canvas).Stretch();
            var hud = root.gameObject.AddComponent<CombatHUD>();
            hud.Build(root, showKeyboardHint);
            return hud;
        }

        public void Bind(Combatant player, Combatant enemy)
        {
            this.player = player;
            this.enemy = enemy;
            playerCaster = player.GetComponent<AbilityCaster>();

            playerPanel.Name.text = Title(player);
            enemyPanel.Name.text = Title(enemy);

            for (int i = 0; i < skills.Length; i++)
            {
                var move = playerCaster.GetMove(i);
                skills[i].Background.color = move.Type.TypeColor();
                skills[i].Label.text = move.Name;
            }
        }

        static string Title(Combatant c)
        {
            string types = string.Join(" / ", System.Array.ConvertAll(c.Species.Types, t => t.DisplayName()));
            return $"{c.Species.Name}  Nv. {c.Stats.Level}   <size=22>{types}</size>";
        }

        void Update()
        {
            if (player == null || enemy == null) return;

            UpdatePanel(playerPanel, player);
            UpdatePanel(enemyPanel, enemy);

            var motor = player.Motor;
            UIFactory.SetBarFill(playerPanel.StaminaFill, motor.Stamina / CombatTuning.MaxStamina);
            dodgeBackground.color = motor.Stamina >= CombatTuning.DodgeCost
                ? new Color(0.35f, 0.75f, 1f)
                : new Color(0.25f, 0.3f, 0.35f);

            for (int i = 0; i < skills.Length; i++)
            {
                float fraction = playerCaster.CooldownFraction(i);
                skills[i].Cooldown.fillAmount = fraction;
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

        void Build(RectTransform root, bool showKeyboardHint)
        {
            playerPanel = BuildPanel(root, new Vector2(0f, 1f), new Vector2(30f, -30f), withStamina: true);
            enemyPanel = BuildPanel(root, new Vector2(1f, 1f), new Vector2(-30f, -30f), withStamina: false);

            BuildJoystick(root);

            var bottomRight = new Vector2(1f, 0f);
            AbilityButtons = new AbilityButton[AbilityCaster.SlotCount];
            skills = new SkillView[AbilityCaster.SlotCount];
            Vector2[] positions = { new Vector2(-110f, 90f), new Vector2(-400f, 70f), new Vector2(-370f, 330f) };
            float[] sizes = { 220f, 150f, 150f };
            for (int i = 0; i < AbilityCaster.SlotCount; i++)
                AbilityButtons[i] = BuildSkillButton(root, i, bottomRight, positions[i], sizes[i]);

            DodgeButton = BuildDodgeButton(root, bottomRight, new Vector2(-90f, 360f), 130f);

            if (showKeyboardHint)
            {
                var hint = UIFactory.CreateText("Ayuda teclado", root,
                    "WASD mover · J ataque · K / L habilidades · Espacio esquivar",
                    22, new Color(1f, 1f, 1f, 0.7f), TextAnchor.LowerCenter);
                hint.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(900f, 40f));
            }
        }

        static Panel BuildPanel(RectTransform root, Vector2 anchor, Vector2 position, bool withStamina)
        {
            var bg = UIFactory.CreateImage("Panel", root, PanelColor);
            bg.raycastTarget = false;
            var rect = bg.rectTransform.Place(anchor, position, new Vector2(560f, withStamina ? 140f : 110f));

            var panel = new Panel();
            var align = anchor.x < 0.5f ? TextAnchor.UpperLeft : TextAnchor.UpperRight;

            panel.Name = UIFactory.CreateText("Nombre", rect, "", 32, Color.white, align);
            panel.Name.supportRichText = true;
            panel.Name.rectTransform.Place(new Vector2(anchor.x, 1f), new Vector2(anchor.x < 0.5f ? 16f : -16f, -10f), new Vector2(520f, 40f));

            panel.Status = UIFactory.CreateText("Estado", rect, "", 24, new Color(1f, 0.55f, 0.3f),
                anchor.x < 0.5f ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
            panel.Status.rectTransform.Place(new Vector2(anchor.x, 1f), new Vector2(anchor.x < 0.5f ? 16f : -16f, -14f), new Vector2(520f, 34f));

            var hpBar = UIFactory.CreateBar("Vida", rect, BarBackground, Color.green);
            panel.HpFill = hpBar;
            ((RectTransform)hpBar.parent).Place(new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(528f, 32f));
            panel.HpText = UIFactory.CreateText("PS", hpBar.parent, "", 22, Color.white, TextAnchor.MiddleCenter);
            panel.HpText.rectTransform.Stretch();

            if (withStamina)
            {
                var stamina = UIFactory.CreateBar("Estamina", rect, BarBackground, new Color(0.35f, 0.75f, 1f));
                panel.StaminaFill = stamina;
                ((RectTransform)stamina.parent).Place(new Vector2(0.5f, 1f), new Vector2(0f, -102f), new Vector2(528f, 16f));
            }

            return panel;
        }

        void BuildJoystick(RectTransform root)
        {
            // Zona invisible que recibe el toque: mitad izquierda, debajo de los paneles.
            var zone = UIFactory.CreateImage("Zona joystick", root, new Color(0f, 0f, 0f, 0f));
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

        AbilityButton BuildSkillButton(RectTransform root, int slot, Vector2 anchor, Vector2 position, float size)
        {
            var bg = UIFactory.CreateImage($"Habilidad {slot}", root, Color.gray, UIFactory.Circle);
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

        AbilityButton BuildDodgeButton(RectTransform root, Vector2 anchor, Vector2 position, float size)
        {
            dodgeBackground = UIFactory.CreateImage("Esquiva", root, new Color(0.35f, 0.75f, 1f), UIFactory.Circle);
            dodgeBackground.alphaHitTestMinimumThreshold = 0.1f;
            dodgeBackground.rectTransform.Place(anchor, position, Vector2.one * size);

            var label = UIFactory.CreateText("Texto", dodgeBackground.transform, "Esquivar", 24, Color.white, TextAnchor.MiddleCenter);
            label.rectTransform.Stretch();

            return dodgeBackground.gameObject.AddComponent<AbilityButton>();
        }
    }
}
