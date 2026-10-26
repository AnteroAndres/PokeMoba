using System;
using System.Collections.Generic;
using System.Linq;
using PokeMoba.Data;
using UnityEngine;
using UnityEngine.UI;

namespace PokeMoba.UI
{
    /// <summary>Cuadrícula desplazable con todas las especies para elegir una.</summary>
    public static class SpeciesPicker
    {
        const int Columns = 7;
        static readonly Vector2 CellSize = new Vector2(230f, 120f);
        static readonly Vector2 Spacing = new Vector2(14f, 14f);

        /// <summary>Construye la cuadrícula dentro de <paramref name="parent"/>, ocupando el área indicada en píxeles del canvas.</summary>
        public static RectTransform Build(Transform parent, IReadOnlyList<SpeciesData> species,
            Action<SpeciesData> onPick, float top, float bottom)
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
                string types = string.Join(" / ", s.Types.Select(t => t.DisplayName()));
                UIFactory.CreateButton(s.Key ?? s.Name, content,
                    $"<size=20>#{s.DexNumber:000}</size>\n{s.Name}\n<size=18>{types}</size>", 28,
                    Color.Lerp(s.Types[0].TypeColor(), Color.black, 0.15f), () => onPick(current));
            }

            return viewportRect;
        }
    }
}
