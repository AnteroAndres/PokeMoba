using System;
using System.Collections.Generic;
using System.Linq;
using PokeMoba.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Cómo se muestra una especie en la cuadrícula.</summary>
    public enum PickerState
    {
        Normal,   // nombre, tipos y color
        Seen,     // nombre en gris (Pokédex: visto pero no capturado)
        Unknown   // "???" (Pokédex: no visto); no se puede tocar
    }

    /// <summary>Cuadrícula desplazable con especies para elegir una (selección de Pokémon y Pokédex).</summary>
    public static class SpeciesPicker
    {
        const int Columns = 7;
        static readonly Vector2 CellSize = new Vector2(230f, 120f);
        static readonly Vector2 Spacing = new Vector2(14f, 14f);

        /// <summary>Construye la cuadrícula dentro de <paramref name="parent"/>, dejando libres <paramref name="top"/> y <paramref name="bottom"/> píxeles del canvas.</summary>
        public static RectTransform Build(Transform parent, IEnumerable<SpeciesData> species,
            Action<SpeciesData> onPick, float top, float bottom, Func<SpeciesData, PickerState> stateOf = null)
        {
            float width = Columns * CellSize.x + (Columns - 1) * Spacing.x + 40f;

            // Ventana visible (máscara)
            var viewport = UIFactory.CreateImage("Lista", parent, new Color(1f, 1f, 1f, 0.05f));
            var viewportRect = viewport.rectTransform;
            viewportRect.anchorMin = new Vector2(0.5f, 0f);
            viewportRect.anchorMax = new Vector2(0.5f, 1f);
            viewportRect.pivot = new Vector2(0.5f, 1f);
            viewportRect.sizeDelta = new Vector2(width, -(top + bottom));
            viewportRect.anchoredPosition = new Vector2(0f, -top);
            viewport.gameObject.AddComponent<RectMask2D>();

            // Contenido que crece con el número de especies
            var content = UIFactory.CreateRect("Contenido", viewportRect);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = CellSize;
            grid.spacing = Spacing;
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Columns;
            grid.childAlignment = TextAnchor.UpperCenter;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            foreach (var s in species)
            {
                var current = s;
                var state = stateOf != null ? stateOf(s) : PickerState.Normal;
                string types = string.Join(" / ", s.Types.Select(t => t.DisplayName()));

                string label;
                Color color;
                switch (state)
                {
                    case PickerState.Unknown:
                        label = $"<size=20>#{s.DexNumber:000}</size>\n???";
                        color = new Color(0.12f, 0.12f, 0.15f);
                        break;
                    case PickerState.Seen:
                        label = $"<size=20>#{s.DexNumber:000}</size>\n{s.Name}\n<size=18>visto</size>";
                        color = new Color(0.3f, 0.3f, 0.34f);
                        break;
                    default:
                        label = $"<size=20>#{s.DexNumber:000}</size>\n{s.Name}\n<size=18>{types}</size>";
                        color = Color.Lerp(s.Types[0].TypeColor(), Color.black, 0.15f);
                        break;
                }

                var button = UIFactory.CreateButton(s.Key ?? s.Name, content, label, 28, color, () => onPick(current));
                button.GetComponentInChildren<Text>().supportRichText = true;
                button.interactable = state != PickerState.Unknown;
            }

            return viewportRect;
        }
    }
}
