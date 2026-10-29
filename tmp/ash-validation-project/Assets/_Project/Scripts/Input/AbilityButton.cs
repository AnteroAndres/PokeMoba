using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PokeMoba.Input
{
    /// <summary>
    /// Botón de habilidad: un toque lanza con apuntado automático; arrastrar apunta a mano
    /// y al soltar se lanza en esa dirección.
    /// </summary>
    public class AbilityButton : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const int NoPointer = int.MinValue;
        const float DeadZone = 25f;   // unidades del canvas antes de considerar que se está apuntando
        const float MaxDrag = 140f;   // arrastre que equivale al alcance máximo

        public Action Tapped;
        /// <summary>Vector de apuntado normalizado (magnitud 0–1) mientras se arrastra.</summary>
        public Action<Vector2> AimChanged;
        public Action<Vector2> AimReleased;

        int pointerId = NoPointer;
        bool aiming;
        Vector2 start;
        Canvas canvas;

        void Awake() => canvas = GetComponentInParent<Canvas>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer) return;
            pointerId = eventData.pointerId;
            start = eventData.position;
            aiming = false;
            transform.localScale = Vector3.one * 0.92f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            Vector2 aim = AimVector(eventData.position, out float distance);
            if (!aiming && distance > DeadZone) aiming = true;
            if (aiming) AimChanged?.Invoke(aim);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            pointerId = NoPointer;
            transform.localScale = Vector3.one;

            if (aiming) AimReleased?.Invoke(AimVector(eventData.position, out _));
            else Tapped?.Invoke();
            aiming = false;
        }

        void OnDisable()
        {
            pointerId = NoPointer;
            aiming = false;
            transform.localScale = Vector3.one;
        }

        Vector2 AimVector(Vector2 screenPosition, out float distance)
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 delta = (screenPosition - start) / scale;
            distance = delta.magnitude;
            return Vector2.ClampMagnitude(delta / MaxDrag, 1f);
        }
    }
}
