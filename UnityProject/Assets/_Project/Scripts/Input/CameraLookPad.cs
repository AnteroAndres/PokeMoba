using PokeMoba.Prototype;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PokeMoba.Input
{
    /// <summary>Zona táctil de cámara independiente del joystick y de los botones.</summary>
    public class CameraLookPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int pointer = int.MinValue;
        public void OnPointerDown(PointerEventData e)
        {
            if (pointer == int.MinValue) pointer = e.pointerId;
        }
        public void OnDrag(PointerEventData e)
        {
            if (pointer != e.pointerId || Time.timeScale <= 0f || Camera.main == null) return;
            var camera = Camera.main.GetComponent<FollowCamera>();
            if (camera != null) camera.RotateLook(e.delta * (120f / Mathf.Max(1, Screen.height)));
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (pointer == e.pointerId) pointer = int.MinValue;
        }
        void OnDisable() => pointer = int.MinValue;
    }
}
