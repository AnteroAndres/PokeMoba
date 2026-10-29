using UnityEngine;
using UnityEngine.EventSystems;

namespace PokeMoba.Input
{
    /// <summary>
    /// Joystick flotante: va en una zona invisible (mitad izquierda de la pantalla)
    /// y aparece donde se apoya el dedo.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const int NoPointer = int.MinValue;

        public Vector2 Value { get; private set; }

        RectTransform background;
        RectTransform knob;
        Vector2 restPosition;
        float radius;
        int pointerId = NoPointer;
        Canvas canvas;

        public void Setup(RectTransform background, RectTransform knob, float radius)
        {
            this.background = background;
            this.knob = knob;
            this.radius = radius;
            restPosition = background.anchoredPosition;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer) return;
            pointerId = eventData.pointerId;
            // En un Canvas Screen Space - Overlay la posición del RectTransform está en píxeles de pantalla.
            background.position = eventData.position;
            UpdateValue(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            UpdateValue(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId) return;
            pointerId = NoPointer;
            Value = Vector2.zero;
            knob.anchoredPosition = Vector2.zero;
            background.anchoredPosition = restPosition;
        }

        void OnDisable()
        {
            pointerId = NoPointer;
            Value = Vector2.zero;
        }

        void UpdateValue(Vector2 screenPosition)
        {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            Vector2 offset = (screenPosition - (Vector2)background.position) / (radius * scale);
            Value = Vector2.ClampMagnitude(offset, 1f);
            knob.anchoredPosition = Value * radius;
        }
    }
}
