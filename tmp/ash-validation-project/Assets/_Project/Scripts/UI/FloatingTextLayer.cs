using System.Collections.Generic;
using PokeMoba.Combat;
using PokeMoba.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Números de daño y avisos ("¡Súper eficaz!", "¡Esquivado!") que flotan sobre los Pokémon.</summary>
    public class FloatingTextLayer : MonoBehaviour
    {
        const float Lifetime = 0.9f;

        class Entry
        {
            public Text Text;
            public Vector3 WorldPosition;
            public float Age;
        }

        readonly List<Entry> entries = new List<Entry>();
        Camera cam;
        RectTransform rect;

        public void Setup(Camera camera)
        {
            cam = camera;
            rect = (RectTransform)transform;
        }

        void OnEnable()
        {
            Combatant.AnyDamaged += OnDamaged;
            Combatant.AnyDodged += OnDodged;
            Combatant.AnyStatusApplied += OnStatus;
        }

        void OnDisable()
        {
            Combatant.AnyDamaged -= OnDamaged;
            Combatant.AnyDodged -= OnDodged;
            Combatant.AnyStatusApplied -= OnStatus;
        }

        void OnDamaged(Combatant target, DamageResult result)
        {
            if (result.FromStatus)
            {
                Spawn(target, result.Damage.ToString(), new Color(0.75f, 0.45f, 0.9f), 34);
                return;
            }

            string label = result.Damage.ToString();
            Color color = Color.white;
            int size = 44;

            if (result.Effectiveness <= 0f) { label = "No afecta"; color = Color.gray; }
            else if (result.Effectiveness >= 2f) { label += "\n¡Súper eficaz!"; color = new Color(1f, 0.55f, 0.1f); size = 52; }
            else if (result.Effectiveness < 1f) { label += "\nPoco eficaz"; color = new Color(0.7f, 0.7f, 0.7f); size = 36; }

            if (result.Critical) label += "\n¡Crítico!";

            Spawn(target, label, color, size);
        }

        void OnDodged(Combatant target) => Spawn(target, "¡Esquivado!", new Color(0.4f, 0.9f, 1f), 40);

        void OnStatus(Combatant target, StatusEffect status) =>
            Spawn(target, $"¡{status}!", status == StatusEffect.Quemado ? new Color(1f, 0.4f, 0.2f) : new Color(0.7f, 0.3f, 0.8f), 40);

        void Spawn(Combatant target, string content, Color color, int size)
        {
            var text = UIFactory.CreateText("Flotante", transform, content, size, color, TextAnchor.MiddleCenter);
            text.rectTransform.Place(new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 100f));
            Vector3 jitter = new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.2f, 0.2f));
            entries.Add(new Entry { Text = text, WorldPosition = target.transform.position + Vector3.up * 2.4f + jitter });
        }

        void LateUpdate()
        {
            if (cam == null) return;

            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                entry.Age += Time.deltaTime;
                if (entry.Age >= Lifetime || entry.Text == null)
                {
                    if (entry.Text != null) Destroy(entry.Text.gameObject);
                    entries.RemoveAt(i);
                    continue;
                }

                float t = entry.Age / Lifetime;
                Vector3 screen = cam.WorldToScreenPoint(entry.WorldPosition + Vector3.up * t * 1.2f);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, null, out Vector2 local);
                entry.Text.rectTransform.anchoredPosition = local;

                var c = entry.Text.color;
                c.a = 1f - t * t;
                entry.Text.color = c;
            }
        }
    }
}
